using BotLooter.Resources;
using BotLooter.Steam.Contracts;

namespace BotLooter.Looting;

public class LootReceiverPicker
{
    private readonly Configuration _configuration;

    public LootReceiverPicker(Configuration configuration)
    {
        _configuration = configuration;
    }

    public TradeOfferUrl Pick() 
        => _configuration.LootTradeOfferUrl ??
           _configuration.LootTradeOfferUrls![Random.Shared.Next(_configuration.LootTradeOfferUrls.Count)];
}