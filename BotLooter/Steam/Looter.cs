using System.Net;
using BotLooter.Resources;
using BotLooter.Steam.Contracts;
using BotLooter.Steam.Contracts.Responses;
using Polly;
using Polly.Retry;
using RestSharp;

namespace BotLooter.Steam;

public class Looter
{
    private readonly AsyncRetryPolicy<RestResponse<GetInventoryResponse>> _getInventoryPolicy;

    public Looter()
    {
        _getInventoryPolicy = Policy
            .HandleResult<RestResponse<GetInventoryResponse>>(x => x.Data is null)
            .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(10));
    }

    public async Task Loot(List<SteamAccountCredentials> accountCredentials, ProxyPool proxyPool, TradeOfferUrl tradeOfferUrl, int delaySeconds)
    {
        var counter = 0;
        
        foreach (var credentials in accountCredentials)
        {
            if (++counter != 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }

            var prefix = $"{counter}/{accountCredentials.Count} {credentials.Login}:";
            
            var restClient = proxyPool.Provide();

            var session = new SteamSession(credentials, restClient);

            if (!await session.TryEnsureSession())
            {
                Console.WriteLine($"{prefix} Не смог получить валидную сессию");
                continue;
            }
            
            var web = new SteamWeb(session);
        
            var (assets, getAssetsMessage) = await GetAssetsToSend(web, credentials.SteamGuardAccount.Session.SteamID);

            if (assets is null)
            {
                Console.WriteLine($"{prefix} {getAssetsMessage}");
                continue;
            }

            var tradeOffer = new JsonTradeOffer
            {
                NewVersion = true,
                Version = 4
            };
        
            foreach (var inventoryAsset in assets)
            {
                var asset = new TradeOfferAsset
                {
                    AppId = $"{inventoryAsset.Appid}",
                    ContextId = $"{inventoryAsset.Contextid}",
                    Amount = 1,
                    AssetId = inventoryAsset.Assetid
                };
            
                tradeOffer.Me.Assets.Add(asset);
            }

            var sendTradeOfferResponse = await web.SendTradeOffer(tradeOfferUrl, tradeOffer);

            if (sendTradeOfferResponse.StatusCode != HttpStatusCode.OK ||
                sendTradeOfferResponse.Data is not {} sendTradeOfferData || 
                !ulong.TryParse(sendTradeOfferData.TradeofferId, out var tradeOfferId))
            {
                Console.WriteLine($"{prefix} Не смог отправить обмен - {sendTradeOfferResponse.StatusCode} {sendTradeOfferResponse.Content}");
                continue;
            }

            var confirmationResult = await session.AcceptConfirmation(tradeOfferId);

            if (!confirmationResult)
            {
                Console.WriteLine($"{prefix} Не смог подтвердить обмен");
                continue;
            }
            
            Console.WriteLine($"{prefix} Залутан! Предметов: {tradeOffer.Me.Assets.Count}");
        }
    }

    private async Task<(List<Asset>? Assets, string message)> GetAssetsToSend(SteamWeb web, ulong steamId64)
    {
        var inventoryResponse = await _getInventoryPolicy.ExecuteAsync(() => web.GetInventory(steamId64, 730, 2));

        if (inventoryResponse.Data is not { } inventoryData)
        {
            return (null, $"Не смог получить инвентарь. StatusCode: {inventoryResponse.StatusCode}");
        }

        if (inventoryData.Assets is null || inventoryData.Assets.Count < 1)
        {
            return (null, "Пустой инвентарь");
        }

        return (inventoryData.Assets, "");
    }
}