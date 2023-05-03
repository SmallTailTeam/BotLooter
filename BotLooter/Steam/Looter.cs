using System.Net;
using BotLooter.Resources;
using BotLooter.Steam.Contracts;

namespace BotLooter.Steam;

public class Looter
{
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
        
            var inventoryResponse = await web.GetInventory(credentials.SteamGuardAccount.Session.SteamID, 730, 2);

            if (inventoryResponse.Data is not { } inventoryData)
            {
                Console.WriteLine($"{prefix} Не смог получить инвентарь - {inventoryResponse.StatusCode}");
                continue;
            }

            if (inventoryData.Assets is null || inventoryData.Assets.Count < 1)
            {
                Console.WriteLine($"{prefix} Пустой инвентарь");
                continue;
            }

            var tradeOffer = new JsonTradeOffer
            {
                NewVersion = true,
                Version = 4
            };
        
            foreach (var inventoryAsset in inventoryData.Assets)
            {
                var asset = new TradeOfferAsset
                {
                    AppId = "730",
                    ContextId = "2",
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
}