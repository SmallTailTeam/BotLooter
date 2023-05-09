using BotLooter.Resources;
using BotLooter.Steam.Contracts;
using Serilog;

namespace BotLooter.Steam;

public class Looter
{
    private readonly ILogger _logger;

    public Looter(ILogger logger)
    {
        _logger = logger;
    }

    public async Task Loot(List<LootClient> lootClients, TradeOfferUrl tradeOfferUrl, Configuration config)
    {
        _logger.Information("Начинаю лутать. Потоков: {ThreadCount}", config.LootThreadCount);
    
        var counter = 0;

        await Parallel.ForEachAsync(lootClients, new ParallelOptions
        {
            MaxDegreeOfParallelism = config.LootThreadCount
        }, async (lootClient, _) =>
        {
            var lootResult = await lootClient.TryLoot(tradeOfferUrl);

            Interlocked.Increment(ref counter);
            var progress = $"{counter}/{lootClients.Count}";
            
            var identifier = $"{lootClient.Credentials.Login}";
            
            _logger.Information($"{progress} | {identifier} | {lootResult.Message}");

            await WaitForNextLoot(lootResult.Message, config);
        });
        
        _logger.Information("Лутание завершено");
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