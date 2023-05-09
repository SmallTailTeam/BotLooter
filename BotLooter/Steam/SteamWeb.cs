using AngleSharp.Html.Parser;
using BotLooter.Steam.Contracts;
using BotLooter.Steam.Contracts.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace BotLooter.Steam;

public class SteamWeb
{
    private readonly SteamSession _session;
    private readonly IHtmlParser _htmlParser;

    public SteamWeb(SteamSession session)
    {
        _session = session;
        _htmlParser = new HtmlParser();
    }
    
    public async Task<RestResponse<GetInventoryResponse>> GetInventory(ulong steamId64, string inventoryId, string contextId, int count = 100)
    {
        var request = new RestRequest($"https://steamcommunity.com/inventory/{steamId64}/{inventoryId}/{contextId}");
        request.AddParameter("l", "english");
        request.AddParameter("count", count);

        return await _session.WebRequest<GetInventoryResponse>(request);
    }
    
    public async Task<RestResponse<SendTradeOfferResponse>> SendTradeOffer(TradeOfferUrl tradeOfferUrl, JsonTradeOffer jsonTradeOffer)
    {
        var request = new RestRequest(tradeOfferUrl.ToString());
        await _session.WebRequest(request);
        
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
        
        return await _session.WebRequest<SendTradeOfferResponse>(request, true);
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

        var response = await _session.WebRequest(request);

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