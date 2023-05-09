using System.Net;
using BotLooter.Resources;
using BotLooter.Steam.Contracts;
using BotLooter.Steam.Contracts.Responses;
using Polly;
using Polly.Retry;
using RestSharp;

namespace BotLooter.Steam;

public class LootClient
{
    public SteamAccountCredentials Credentials { get; }
    
    private readonly SteamSession _steamSession;
    private readonly SteamWeb _steamWeb;
    
    private readonly AsyncRetryPolicy<RestResponse<GetInventoryResponse>> _getInventoryPolicy;

    public LootClient(SteamAccountCredentials credentials, SteamSession steamSession, SteamWeb steamWeb)
    {
        Credentials = credentials;
        _steamSession = steamSession;
        _steamWeb = steamWeb;
        
        _getInventoryPolicy = Policy
            .HandleResult<RestResponse<GetInventoryResponse>>(x => x.Data is null)
            .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(10));
    }

    public async Task<(int? LootedItemCount, string Message)> TryLoot(TradeOfferUrl tradeOfferUrl)
    {
        var (isSession, ensureSessionMessage) = await _steamSession.TryEnsureSession();

        if (!isSession)
        {
            return (null, ensureSessionMessage);
        }

        var (assets, getAssetsMessage) = await GetAssetsToSend(_steamWeb, Credentials.SteamGuardAccount.Session.SteamID);

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

        var (tradeOfferId, sendTradeOfferMessage) = await SendTradeOffer(tradeOfferUrl, tradeOffer);

        if (tradeOfferId is null)
        {
            return (null, sendTradeOfferMessage);
        }

        var confirmationResult = await _steamSession.AcceptConfirmation(tradeOfferId.Value);

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

    private async Task<(ulong? TradeOfferId, string Message)> SendTradeOffer(TradeOfferUrl tradeOfferUrl, JsonTradeOffer tradeOffer)
    {
        var sendTradeOfferResponse = await _steamWeb.SendTradeOffer(tradeOfferUrl, tradeOffer);

        if (sendTradeOfferResponse.Data is not { } sendTradeOfferData)
        {
            return (null, $"Не смог отправить обмен - {sendTradeOfferResponse.StatusCode} {sendTradeOfferResponse.Content}");
        }

        if (!string.IsNullOrWhiteSpace(sendTradeOfferData.Error))
        {
            if (sendTradeOfferData.Error.Contains("Steam Guard enabled for at least 15 days"))
            {
                var cantTradeTime = await _steamWeb.GetHelpWhyCantITradeTime();

                return (null, $"Обмен будет доступен через {cantTradeTime}");
            }
            
            return (null, $"Не смог отправить обмен - {sendTradeOfferData.Error}");
        }

        if (!ulong.TryParse(sendTradeOfferData.TradeofferId, out var tradeOfferId))
        {
            return (null, $"Не смог спарсить айди обмена - {sendTradeOfferResponse.StatusCode} {sendTradeOfferResponse.Content}");
        }

        return (tradeOfferId, "");
    }
}