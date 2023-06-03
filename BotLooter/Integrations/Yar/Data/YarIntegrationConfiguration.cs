namespace BotLooter.Integrations.Yar.Data;

public class YarIntegrationConfiguration
{
    public string DatabaseFilePath { get; set; }
    public int RewardCheckIntervalSeconds { get; set; } = 5;
}