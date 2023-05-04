using BotLooter.Resources;
using BotLooter.Steam.Contracts;

namespace BotLooter.Steam;

public class Looter
{
    public async Task Loot(List<SteamAccountCredentials> accountCredentials, ProxyPool proxyPool, TradeOfferUrl tradeOfferUrl, Configuration config)
    {
        var counter = 0;
        
        foreach (var credentials in accountCredentials)
        {
            counter++;

            var restClient = proxyPool.Provide();
            
            var steamSession = new SteamSession(credentials, restClient);
            var steamWeb = new SteamWeb(steamSession);
            var lootClient = new LootClient(credentials, steamSession, steamWeb);
            
            var lootResult = await lootClient.TryLoot(tradeOfferUrl);

            var prefix = $"{counter}/{accountCredentials.Count} {credentials.Login}:";
            
            Console.WriteLine($"{prefix} {lootResult.Message}");

            await WaitForNextLoot(lootResult.Message, config);
        }
    }

    private async Task WaitForNextLoot(string message, Configuration config)
    {
        if (message == "Пустой инвентарь")
        {
            await Task.Delay(TimeSpan.FromSeconds(config.DelayInventoryEmptySeconds));
        }
        else
        {
            await Task.Delay(TimeSpan.FromSeconds(config.DelayBetweenAccountsSeconds));
        }
    }
}