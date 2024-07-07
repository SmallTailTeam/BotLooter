using Newtonsoft.Json;

namespace BotLooter.Steam.Contracts.Responses;

public class FinalizeLoginParams
{
    [JsonProperty("nonce")]
    public string Nonce { get; set; }

    [JsonProperty("auth")]
    public string Auth { get; set; }
}

public class FinalizeLoginResponse
{
    [JsonProperty("steamID")]
    public string SteamID { get; set; }

    [JsonProperty("redir")]
    public string Redir { get; set; }

    [JsonProperty("transfer_info")] 
    public List<FinalizeLoginTransferInfo> TransferInfo { get; set; }

    [JsonProperty("primary_domain")]
    public string PrimaryDomain { get; set; }
}

public class FinalizeLoginTransferInfo
{
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("params")]
    public FinalizeLoginParams Params { get; set; }
}