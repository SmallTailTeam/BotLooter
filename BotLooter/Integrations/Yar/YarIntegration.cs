using BotLooter.Integrations.Yar.Data;
using BotLooter.Resources;

namespace BotLooter.Integrations.Yar;

public class YarIntegration
{
    private readonly Configuration _config;
    private readonly YarIntegrationConfiguration _yarConfig;

    public YarIntegration(Configuration config, YarIntegrationConfiguration yarConfig)
    {
        _config = config;
        _yarConfig = yarConfig;
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
            
            var rewardHandlers = new RewardHandlers();
            
            var rewardWatcher = new RewardWatcher(_yarConfig, rewardHandlers);
            rewardWatcher.StartWatching();
        }
    }
}