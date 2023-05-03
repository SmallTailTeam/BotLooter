using BotLooter.Steam.Contracts;
using Newtonsoft.Json;

namespace BotLooter.Resources;

public class Configuration
{
    public string LootTradeOfferUrl { get; set; }
    public string SecretsDirectoryPath { get; set; } = "secrets";
    public string AccountsFilePath { get; set; } = "accounts.txt";
    public string ProxiesFilePath { get; set; } = "proxies.txt";
    public int DelayBetweenAccountsSeconds { get; set; } = 3;

    public static async Task<(Configuration? Config, string Message)> TryLoadFromFile()
    {
        if (!File.Exists("BotLooter.Config.json"))
        {
            return (null, "Файл 'BotLooter.Config.json' не найден");
        }

        var contents = await File.ReadAllTextAsync("BotLooter.Config.json");

        Configuration config;
        
        try
        {
            var deserialized = JsonConvert.DeserializeObject<Configuration>(contents);

            if (deserialized is null)
            {
                return (null, "Конфиг имеет неверный формат");
            }

            config = deserialized;
        }
        catch
        {
            return (null, "Конфиг имеет неверный формат");
        }
        
        if (new TradeOfferUrl(config.LootTradeOfferUrl) is not { IsValid: true })
        {
            return (null, "Параметр конфига 'LootTradeOfferUrl' не заполнен или заполнен неверно");
        }
            
        return (config, "");
    }
}