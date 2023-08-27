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

    private readonly AsyncRetryPolicy<GetInventoryItemsWithDescriptionsResponse?> _getInventoryItemsWithDescriptionsPolicy;

    public SteamWeb(SteamUserSession userSession)
    {
        _userSession = userSession;
        _htmlParser = new HtmlParser();

        _getInventoryItemsWithDescriptionsPolicy = Policy
            .HandleResult<GetInventoryItemsWithDescriptionsResponse?>(res =>
            {
                if (res is null || (res.Response.Assets is not null && res.Response.Descriptions is null))
                {
                    return true;
                }

                return false;
            })
            .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(2));
    }

    public async Task<(HashSet<Description> Descriptions, HashSet<Asset> Assets)?> LoadInventory(string appId, string contextId)
    {
        string? startAssetId = null;

        var descriptions = new HashSet<Description>();
        var assets = new HashSet<Asset>();
        
        do
        {
            var inventoryResponse = await _getInventoryItemsWithDescriptionsPolicy.ExecuteAsync(() => GetInventoryItemsWithDescriptions(appId, contextId, startAssetId));

            if (inventoryResponse is null)
            {
                return null;
            }

            // bad response
            if (inventoryResponse.Response.Assets is not null && inventoryResponse.Response.Descriptions is null)
            {
                return null;
            }

            startAssetId = inventoryResponse.Response.LastAssetId;

            if (inventoryResponse.Response.Descriptions is not null)
            {
                foreach (var description in inventoryResponse.Response.Descriptions)
                {
                    descriptions.Add(description);
                }
            }

            if (inventoryResponse.Response.Assets is not null)
            {
                foreach (var asset in inventoryResponse.Response.Assets)
                {
                    assets.Add(asset);
                }
            }

        } while (startAssetId is not null);

        return (descriptions, assets);
    }
    
    public async Task<GetInventoryItemsWithDescriptionsResponse?> GetInventoryItemsWithDescriptions(
        string appId, 
        string contextId,
        string? startAssetId = null)
    {
        if (_userSession.SteamId is null || _userSession.AccessToken is null)
        {
            return null;
        }
        
        var request = new RestRequest("https://api.steampowered.com/IEconService/GetInventoryItemsWithDescriptions/v1/");
        request.AddParameter("steamid", _userSession.SteamId.Value);
        request.AddParameter("appid", appId);
        request.AddParameter("contextid", contextId);
        request.AddParameter("count", "2000");
        request.AddParameter("get_descriptions", "true");
        request.AddParameter("access_token", _userSession.AccessToken);

        if (startAssetId is not null)
        {
            request.AddParameter("start_assetid", startAssetId);
        }

        var response = await _userSession.WebRequest<GetInventoryItemsWithDescriptionsResponse>(request);
        
        return response.Data;
    }
    
    public async Task<RestResponse<SendTradeOfferResponse>> SendTradeOffer(TradeOfferUrl tradeOfferUrl, JsonTradeOffer jsonTradeOffer)
    {
        var request = new RestRequest(tradeOfferUrl.ToString());
        await _userSession.WebRequest(request);
        
        request = new RestRequest("https://steamcommunity.com/tradeoffer/new/send", Method.Post);
        request.AddHeader("Referer", $"https://steamcommunity.com/tradeoffer/new/?partner={tradeOfferUrl.Partner}");;
        
        request.AddParameter("serverid", "1");
        request.AddParameter("partner", tradeOfferUrl.SteamId64);
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
        request.AddHeader("Accept", "*/*");

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