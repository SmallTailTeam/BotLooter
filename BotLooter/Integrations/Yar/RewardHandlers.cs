using BotLooter.Integrations.Yar.Database.Entities;
using BotLooter.Looting;
using BotLooter.Resources;
using Serilog;

namespace BotLooter.Integrations.Yar;

public class RewardHandlers
{
    private readonly ILogger _logger;
    private readonly Configuration _config;
    private readonly List<LootClient> _lootClients;

    public RewardHandlers(ILogger logger, Configuration config, List<LootClient> lootClients)
    {
        _logger = logger;
        _config = config;
        _lootClients = lootClients;
    }

    public Task Handle(RewardEntity reward)
        => Task.WhenAll(
            new LootRewardHandler(_logger, _config, _lootClients).Handle(reward)
        );
}