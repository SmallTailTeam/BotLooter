using BotLooter.Integrations.Yar.Data;
using BotLooter.Looting;
using BotLooter.Resources;
using Serilog;

namespace BotLooter.Integrations.Yar;

public class YarIntegration
{
    private readonly ILogger _logger;
    private readonly Configuration _config;
    private readonly YarIntegrationConfiguration _yarConfig;
    private readonly List<LootClient> _lootClients;

    public YarIntegration(ILogger logger, Configuration config, YarIntegrationConfiguration yarConfig, List<LootClient> lootClients)
    {
        _logger = logger;
        _config = config;
        _yarConfig = yarConfig;
        _lootClients = lootClients;
    }

    public async Task Integrate()
    {
        if (_config.Mode == "Yar/WatchRewards")
        {
            if (!File.Exists(_yarConfig.DatabaseFilePath))
            { 
                FlowUtils.AbortWithError($"Файл базы данных YAR '{_yarConfig.DatabaseFilePath}' не существует");
                return;
            }
            
            _logger.Information("Начинаю следить за дропами");
            
            var rewardHandlers = new RewardHandlers(_logger, _config, _lootClients);
            
            var rewardWatcher = new RewardWatcher(_yarConfig, rewardHandlers);
            rewardWatcher.StartWatching();

            await Task.Delay(-1);
        }
    }
}