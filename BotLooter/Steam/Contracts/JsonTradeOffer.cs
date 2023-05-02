using Newtonsoft.Json;

namespace BotLooter.Steam.Contracts;

public class TradeOfferAsset
{
    [JsonProperty("appid")] 
    public string AppId;
    [JsonProperty("contextid")]
    public string ContextId;
    [JsonProperty("amount")]
    public int Amount;
    [JsonProperty("assetid")] 
    public string AssetId;
}

public class TradeofferParticipator
{
    [JsonProperty("assets")]
    public List<TradeOfferAsset> Assets = new();
    [JsonProperty("currency")]
    public List<object> Currency = new ();
    [JsonProperty("ready")]
    public bool Ready;
}
    
public class JsonTradeOffer
{
    [JsonProperty("newversion")]
    public bool NewVersion;
    [JsonProperty("version")] 
    public int Version;
    [JsonProperty("me")] 
    public TradeofferParticipator Me = new();
    [JsonProperty("them")] 
    public TradeofferParticipator Them = new();
}