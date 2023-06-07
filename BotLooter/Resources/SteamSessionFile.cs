using Newtonsoft.Json;

namespace BotLooter.Resources;

public class SteamSessionFile
{
    [JsonProperty("Username")]
    public string Username { get; set; }

    [JsonProperty("Password")]
    public string Password { get; set; }

    [JsonProperty("SteamId")]
    public string SteamId { get; set; }

    [JsonProperty("RefreshToken")]
    public string RefreshToken { get; set; }

    [JsonProperty("SharedSecret")]
    public string SharedSecret { get; set; }

    [JsonProperty("IdentitySecret")]
    public string IdentitySecret { get; set; }

    [JsonProperty("SchemaVersion")]
    public int SchemaVersion { get; set; }
}