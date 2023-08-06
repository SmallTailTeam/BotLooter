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

    [JsonProperty("SharedSecret")]
    public string SharedSecret { get; set; }

    [JsonProperty("IdentitySecret")]
    public string IdentitySecret { get; set; }

    [JsonProperty("WebRefreshToken")]
    public string WebRefreshToken { get; set; }

    [JsonProperty("MobileRefreshToken")]
    public string MobileRefreshToken { get; set; }

    [JsonProperty("DesktopRefreshToken")]
    public string DesktopRefreshToken { get; set; }

    [JsonProperty("SchemaVersion")]
    public int SchemaVersion { get; set; }
}