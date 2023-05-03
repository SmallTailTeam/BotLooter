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

    public async Task Loot(List<SteamAccountCredentials> accountCredentials, ProxyPool proxyPool, TradeOfferUrl tradeOfferUrl, Configuration config)
    {
        var counter = 0;
        
        foreach (var credentials in accountCredentials)
        {
            counter++;
            
            var lootResult = await TryLootAccount(credentials, proxyPool, tradeOfferUrl);

            var prefix = $"{counter}/{accountCredentials.Count} {credentials.Login}:";
            
            Console.WriteLine($"{prefix} {lootResult.Message}");

            await WaitForNextLoot(lootResult.Message, config);
        }
    }

    private async Task<(int? LootedItemCount, string Message)> TryLootAccount(SteamAccountCredentials credentials, ProxyPool proxyPool, TradeOfferUrl tradeOfferUrl)
    {
        var restClient = proxyPool.Provide();

        var session = new SteamSession(credentials, restClient);

        var (isSession, ensureSessionMessage) = await session.TryEnsureSession();

        if (!isSession)
        {
            return (null, ensureSessionMessage);
        }

        var web = new SteamWeb(session);

        var (assets, getAssetsMessage) = await GetAssetsToSend(web, credentials.SteamGuardAccount.Session.SteamID);

        if (assets is null)
        {
            return (null, getAssetsMessage);
        }

        if (assets.Count < 1)
        {
            return (null, "Пустой инвентарь");
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
            sendTradeOfferResponse.Data is not { } sendTradeOfferData ||
            !ulong.TryParse(sendTradeOfferData.TradeofferId, out var tradeOfferId))
        {
            return (null, $"Не смог отправить обмен - {sendTradeOfferResponse.StatusCode} {sendTradeOfferResponse.Content}");
        }

        var confirmationResult = await session.AcceptConfirmation(tradeOfferId);

        if (!confirmationResult)
        {
            return (null, "Не смог подтвердить обмен");
        }

        return (tradeOffer.Me.Assets.Count, $"Залутан! Предметов: {tradeOffer.Me.Assets.Count}");
    }

    private async Task<(List<Asset>? Assets, string message)> GetAssetsToSend(SteamWeb web, ulong steamId64)
    {
        var inventoryResponse = await _getInventoryPolicy.ExecuteAsync(() => web.GetInventory(steamId64, 730, 2));

        if (inventoryResponse.Data is not {} inventoryData)
        {
            return (null, $"Не смог получить инвентарь. StatusCode: {inventoryResponse.StatusCode}");
        }

        if (inventoryResponse.Data.Assets is null)
        {
            return (new List<Asset>(), "");
        }

        return (inventoryData.Assets, "");
    }

    private async Task WaitForNextLoot(string message, Configuration config)
    {
        if (message == "Пустой инвентарь")
        {
            await Task.Delay(TimeSpan.FromSeconds(config.DelayInventoryEmptySeconds));
        }
        else
        {
            await Task.Delay(TimeSpan.FromSeconds(config.DelayBetweenAccountsSeconds));
        }
    }
}