using BotLooter.Resources;
using BotLooter.Steam.Contracts;

namespace BotLooter.Steam;

public class Looter
{
    public async Task Loot(List<LootClient> lootClients, TradeOfferUrl tradeOfferUrl, Configuration config)
    {
        Console.WriteLine("Начинаю лутать...");
    
        var counter = 0;
        
        foreach (var lootClient in lootClients)
        {
            counter++;

            var lootResult = await lootClient.TryLoot(tradeOfferUrl);

            var prefix = $"{counter}/{lootClients.Count} {lootClient.Credentials.Login}:";
            
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