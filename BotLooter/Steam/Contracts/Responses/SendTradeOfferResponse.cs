using System.Text.Json.Serialization;

namespace BotLooter.Steam.Contracts.Responses;

public record SendTradeOfferResponse
{
    [JsonPropertyName("tradeofferid")] 
    public string? TradeofferId { get; set; }
    
    [JsonPropertyName("strError")] 
    public string? Error { get; set; }
}