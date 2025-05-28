using System.Net;
using AngleSharp.Html.Parser;
using BotLooter.Steam.Contracts;
using BotLooter.Steam.Contracts.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;
using RestSharp;

namespace BotLooter.Steam;

public class SteamWeb
{
    private readonly SteamUserSession _userSession;
    private readonly IHtmlParser _htmlParser;

    private readonly int MaxItemsPerInventoryRequest = 1000;

    private readonly AsyncRetryPolicy<RestResponse<InventoryResponse?>> _getInventoryPolicy;

    public SteamWeb(SteamUserSession userSession)
    {
        _userSession = userSession;
        _htmlParser = new HtmlParser();

        _getInventoryPolicy = Policy
            .HandleResult<RestResponse<InventoryResponse?>>(res =>
            {
                if (res.StatusCode != HttpStatusCode.OK || res.Data is null || res.Data.Success != 1)
                {
                    return true;
                }

                // bad response
                if (res.Data.TotalInventoryCount is null || res.Data.TotalInventoryCount > 0 && (res.Data.Assets is null || res.Data.Descriptions is null))
                {
                    return true;
                }

                return false;
            })
            .WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(4));
    }

    public async Task<(HashSet<Description> Descriptions, HashSet<Asset> Assets)?> LoadInventory(string appId, string contextId)
    {
        string? startAssetId = null;

        var descriptions = new HashSet<Description>();
        var assets = new HashSet<Asset>();
        
        do
        {
            var inventoryResponse = await GetInventory(appId, contextId, startAssetId);

            if (inventoryResponse is null)
            {
                return null;
            }
            
            if (inventoryResponse.Descriptions is not null)
            {
                foreach (var description in inventoryResponse.Descriptions)
                {
                    descriptions.Add(description);
                }
            }

            if (inventoryResponse.Assets is not null)
            {
                foreach (var asset in inventoryResponse.Assets)
                {
                    assets.Add(asset);
                }
            }

            startAssetId = inventoryResponse.LastAssetId;
            
        } while (startAssetId is not null);

        return (descriptions, assets);
    }
    
    public async Task<InventoryResponse?> GetInventory(
        string appId, 
        string contextId,
        string? startAssetId = null)
    {
        if (_userSession.SteamId is null)
        {
            return null;
        }
        
        var request = new RestRequest($"https://steamcommunity.com/inventory/{_userSession.SteamId}/{appId}/{contextId}");

        request.AddHeader("Referer", $"https://steamcommunity.com/profiles/{_userSession.SteamId}/inventory");

        request.AddParameter("l", "english");
        request.AddParameter("count", MaxItemsPerInventoryRequest);
        if (startAssetId is not null)
        {
            request.AddParameter("start_assetid", startAssetId);
        }

        var response = await _getInventoryPolicy.ExecuteAsync(async () => await _userSession.WebRequest<InventoryResponse?>(request));

        if (response.StatusCode != HttpStatusCode.OK || response.Data is null || response.Data.Success != 1)
        {
            return null;
        }

        // bad response
        if (response.Data.TotalInventoryCount is null || response.Data.TotalInventoryCount > 0 && (response.Data.Assets is null || response.Data.Descriptions is null))
        {
            return null;
        }
        
        return response.Data;
    }
    
    public async Task<RestResponse<SendTradeOfferResponse>> SendTradeOffer(TradeOfferUrl tradeOfferUrl, JsonTradeOffer jsonTradeOffer)
    {
        var request = new RestRequest(tradeOfferUrl.ToString());
        await _userSession.WebRequest(request);
        
        request = new RestRequest("https://steamcommunity.com/tradeoffer/new/send", Method.Post);
        request.AddHeader("Origin", "https://steamcommunity.com");
        request.AddHeader("Referer", $"https://steamcommunity.com/tradeoffer/new/?partner={tradeOfferUrl.Partner}");
        
        request.AddParameter("serverid", "1");
        request.AddParameter("partner", (SteamId64)tradeOfferUrl.Partner);
        request.AddParameter("tradeoffermessage", "");
        request.AddParameter("captcha", "");
        request.AddParameter("trade_offer_create_params", new JObject
        {
            {"trade_offer_access_token", tradeOfferUrl.Token},
        }.ToString(Formatting.None));
        
        request.AddParameter("json_tradeoffer",JsonConvert.SerializeObject(jsonTradeOffer));
        
        return await _userSession.WebRequest<SendTradeOfferResponse>(request, true);
    }

    public async Task<string> GetHelpWhyCantITradeTime()
    {
        var request = new RestRequest("https://help.steampowered.com/ru/wizard/HelpWhyCantITrade");

        var response = await _userSession.WebRequest(request);

        if (response.Content is null)
        {
            return "";
        }
        
        var document = _htmlParser.ParseDocument(response.Content);
        
        var infoBox = document.QuerySelector(".info_box");
        var value = infoBox?.QuerySelector(".help_highlight_text:last-child")?.TextContent ?? "";

        return value.Replace("Это ограничение будет снято через ", "");
    }
}