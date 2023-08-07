using AngleSharp.Html.Parser;
using BotLooter.Steam.Contracts;
using BotLooter.Steam.Contracts.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace BotLooter.Steam;

public class SteamWeb
{
    private readonly SteamUserSession _userSession;
    private readonly IHtmlParser _htmlParser;

    public SteamWeb(SteamUserSession userSession)
    {
        _userSession = userSession;
        _htmlParser = new HtmlParser();
    }

    public async Task<(HashSet<Description> Descriptions, HashSet<Asset> Assets)?> LoadInventory(string appId, string contextId)
    {
        string? startAssetId = null;

        var descriptions = new HashSet<Description>();
        var assets = new HashSet<Asset>();
        
        do
        {
            var inventoryResponse = await GetInventoryItemsWithDescriptions(appId, contextId, 10000, true, startAssetId);

            if (inventoryResponse is null)
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

            if (startAssetId is not null)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }

        } while (startAssetId is not null);

        return (descriptions, assets);
    }
    
    public async Task<GetInventoryItemsWithDescriptionsResponse?> GetInventoryItemsWithDescriptions(
        string appId, 
        string contextId,
        int count = 10000,
        bool getDescriptions = true,
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
        request.AddParameter("count", count);
        request.AddParameter("get_descriptions", getDescriptions);
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
        request.AddHeader("referer", $"https://steamcommunity.com/tradeoffer/new/?partner={tradeOfferUrl.Partner}");;
        
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
        request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
        request.AddHeader("Accept-Encoding", "gzip, deflate, br");
        request.AddHeader("Accept-Language", "en-US;q=0.5");
        request.AddHeader("Sec-Fetch-Dest", "document");
        request.AddHeader("Sec-Fetch-Mode", "navigate");
        request.AddHeader("Sec-Fetch-Site", "cross-site");

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