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
        if (!File.Exists("Config.json"))
        {
            return (null, "Файл Config.json не найден");
        }

        var contents = await File.ReadAllTextAsync("Config.json");

        try
        {
            var config = JsonConvert.DeserializeObject<Configuration>(contents);

            if (config is null)
            {
                return (null, "Не удалось десериализовать конфиг");
            }

            if (string.IsNullOrWhiteSpace(config.LootTradeOfferUrl))
            {
                return (null, "Параметр конфига 'LootTradeOfferUrl' не заполнен");
            }
            
            return (config, "");
        }
        catch
        {
            return (null, "Не удалось десериализовать конфиг");
        }
    }
}