﻿using BotLooter.Resources;
using BotLooter.Steam;
using BotLooter.Steam.Contracts;
using BotLooter.Steam.Contracts.Responses;
using Polly;
using Polly.Retry;
using RestSharp;
using Serilog;

namespace BotLooter.Looting;

public class LootClient
{
    public SteamAccountCredentials Credentials { get; }
    
    private readonly SteamUserSession _steamSession;
    private readonly SteamWeb _steamWeb;
    
    private readonly AsyncRetryPolicy<RestResponse<GetInventoryResponse>> _getInventoryPolicy;

    public LootClient(SteamAccountCredentials credentials, SteamUserSession steamSession, SteamWeb steamWeb)
    {
        Credentials = credentials;
        _steamSession = steamSession;
        _steamWeb = steamWeb;
        
        _getInventoryPolicy = Policy
            .HandleResult<RestResponse<GetInventoryResponse>>(x => x.Data is null)
            .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(10));
    }

    public async Task<(int? LootedItemCount, string Message)> TryLoot(TradeOfferUrl tradeOfferUrl, List<string> inventories, bool ignoreNotMarketable)
    {
        var (isSession, ensureSessionMessage) = await _steamSession.TryEnsureSession();

        if (!isSession)
        {
            return (null, ensureSessionMessage);
        }
        
        Log.Logger.Debug("{Login} : {SessionType}", Credentials.Login, ensureSessionMessage);

        var (assets, getAssetsMessage) = await GetAssetsToSend(inventories, ignoreNotMarketable);

        if (assets is null)
        {
            return (null, getAssetsMessage);
        }

        if (assets.Count < 1)
        {
            return (null, "Пустые инвентари");
        }

        var tradeOffer = new JsonTradeOffer
        {
            NewVersion = true,
            Version = 4
        };

        foreach (var inventoryAsset in assets)
        {
            if (!int.TryParse(inventoryAsset.Amount, out var amount))
            {
                amount = 1;
            }
            
            var asset = new TradeOfferAsset
            {
                AppId = $"{inventoryAsset.Appid}",
                ContextId = $"{inventoryAsset.Contextid}",
                Amount = amount,
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

    private async Task<(List<Asset>? Assets, string message)> GetAssetsToSend(List<string> inventories, bool ignoreNotMarketable)
    {
        var filteredOut = new HashSet<string>();
        
        var assets = new List<Asset>();

        var index = 0;
        
        foreach (var inventory in inventories)
        {
            var split = inventory.Split('/');

            if (split.Length != 2)
            {
                continue;
            }

            var inventoryId = split[0];
            var contextId = split[1];
            
            var inventoryResponse = await _getInventoryPolicy.ExecuteAsync(() => _steamWeb.GetInventory(Credentials.SteamGuardAccount.Session.SteamID, inventoryId, contextId));
            
            if (inventoryResponse.Data is not {} inventoryData)
            {
                return (null, $"Не смог получить инвентарь {inventory}. StatusCode: {inventoryResponse.StatusCode}");
            }
            
            if (inventoryData.Assets is not {} inventoryAssets)
            {
                continue;
            }
            
            foreach (var description in inventoryData.Descriptions.Where(d => d.Tradable == 0))
            {
                filteredOut.Add(description.Classid);
            }

            if (ignoreNotMarketable)
            {
                foreach (var description in inventoryData.Descriptions.Where(d => d.Marketable == 0))
                {
                    filteredOut.Add(description.Classid);
                }
            }

            var notFilteredOutAssets = inventoryAssets.Where(a => !filteredOut.Contains(a.Classid));
            
            assets.AddRange(notFilteredOutAssets);

            var isLast = index == inventories.Count - 1;
            
            if (!isLast)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
            
            index++;
        }

        return (assets, "");
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