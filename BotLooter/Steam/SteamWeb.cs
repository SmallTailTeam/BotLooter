using BotLooter.Steam.Contracts;
using BotLooter.Steam.Contracts.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace BotLooter.Steam;

public class SteamWeb
{
    private readonly SteamSession _session;

    public SteamWeb(SteamSession session)
    {
        _session = session;
    }
    
    public async Task<RestResponse<GetInventoryResponse>> GetInventory(ulong steamId64, uint appId, uint contextId, int count = 100)
    {
        var request = new RestRequest($"https://steamcommunity.com/inventory/{steamId64}/{appId}/{contextId}");
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
}