using BotLooter.Steam.Contracts;
using Newtonsoft.Json;

namespace BotLooter.Resources;

public class Configuration
{
    public string LootTradeOfferUrl { get; set; } = "";
    public string SecretsDirectoryPath { get; set; } = "";
    public string AccountsFilePath { get; set; } = "";
    public string SteamSessionsDirectoryPath { get; set; } = "";
    public string ProxiesFilePath { get; set; } = "proxies.txt";
    public int DelayBetweenAccountsSeconds { get; set; } = 30;
    public int DelayInventoryEmptySeconds { get; set; } = 0;
    public bool AskForApproval { get; set; } = true;
    public bool ExitOnFinish { get; set; } = false;
    public int LootThreadCount { get; set; } = 1;
    public List<string> Inventories { get; set; } = new();
    public bool IgnoreNotMarketable { get; set; } = false;
    public bool IgnoreMarketable { get; set; } = false;

    public static async Task<(Configuration? Config, string Message)> TryLoadFromFile()
    {
        if (!File.Exists("BotLooter.Config.json"))
        {
            return (null, "Файл 'BotLooter.Config.json' не найден");
        }

        var contents = await File.ReadAllTextAsync("BotLooter.Config.json");

        Configuration config;
        
        var errors = new List<string>();
        
        try
        {
            var deserialized = JsonConvert.DeserializeObject<Configuration>(contents, new JsonSerializerSettings
            {
                Error = (_, args) =>
                {
                    errors.Add(args.ErrorContext.Error.Message);
                }
            });

            if (deserialized is null)
            {
                return (null, "Конфиг имеет неверный формат, подробности:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
            }

            config = deserialized;
        }
        catch
        {
            return (null, "Конфиг имеет неверный формат, подробности:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }
        
        if (new TradeOfferUrl(config.LootTradeOfferUrl) is not { IsValid: true })
        {
            return (null, "Параметр конфига 'LootTradeOfferUrl' не заполнен или заполнен неверно.");
        }

        if (config.Inventories?.Count == 0)
        {
            return (null, """
            В параметре конфига 'Inventories' не указаны инвентари для лута.
            Формат: appId/contextId
            Пример заполнения с инвентарем CS:GO
            ...
            "Inventories": [
                "730/2"
            ],
            ...
            """);
        }
            
        return (config, "");
    }
}