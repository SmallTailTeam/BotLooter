using BotLooter.Integrations.Yar.Database.Entities;
using BotLooter.Looting;
using BotLooter.Resources;
using Serilog;

namespace BotLooter.Integrations.Yar;

public class LootRewardHandler
{
    private readonly ILogger _logger;
    private readonly Configuration _config;
    private readonly List<LootClient> _lootClients;

    public LootRewardHandler(ILogger logger, Configuration config, List<LootClient> lootClients)
    {
        _logger = logger;
        _config = config;
        _lootClients = lootClients;
    }

    public async Task Handle(RewardEntity reward)
    {
        var lootClient = _lootClients.FirstOrDefault(x => x.Credentials.Login.ToLower() == reward.ClientId.ToLower());

        if (lootClient is null)
        {
            _logger.Warning("{Identifier} | Обнаружил дроп, но не смог найти бота в списке", reward.ClientId);
            return;
        }

        var lootResult = await lootClient.TryLoot(_config.LootTradeOfferUrl, _config.Inventories);
        
        _logger.Information("{Identifier} | {Message}", reward.ClientId, lootResult.Message);
    }
}