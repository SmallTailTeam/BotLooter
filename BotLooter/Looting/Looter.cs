using System.Collections.Concurrent;
using BotLooter.Resources;
using Serilog;

namespace BotLooter.Looting;

public class Looter
{
    public event Func<string, LootResult, Task>? OnLooted;
    
    private readonly ILogger _logger;

    public Looter(ILogger logger)
    {
        _logger = logger;
    }

    public async Task Loot(List<LootClient> lootClients, Configuration config)
    {
        _logger.Information("Starting to loot. Threads: {ThreadCount}", config.LootThreadCount);

        var lootResults = new ConcurrentBag<LootResult>();
        
        var counter = 0;
        
        var summaryLootedItems = 0;

        await Parallel.ForEachAsync(
            lootClients,
            new ParallelOptions 
            {
                MaxDegreeOfParallelism = config.LootThreadCount 
            },
            async (lootClient, _) =>
            {
                if (summaryLootedItems >= config.MaxItemsPerAllTrades)
                {
                    return;
                }

                var lootResult = await lootClient.TryLoot(config.LootTradeOfferUrl, config);

                lootResults.Add(lootResult);

                Interlocked.Add(ref summaryLootedItems, lootResult.LootedItemCount);

                OnLooted?.Invoke(lootClient.Credentials.Login, lootResult);

                var progress = $"{Interlocked.Increment(ref counter)}/{lootClients.Count}";

                var identifier = $"{lootClient.Credentials.Login}";

                _logger.Information($"{progress} | {identifier} | {lootResult.Message}");

                await WaitForNextLoot(lootResult, config);
            });
        
        _logger.Information("Looting complete");
        
        ShowResultsSummary(lootResults);
    }

    private async Task WaitForNextLoot(LootResult lootResult, Configuration config)
    {
        if (lootResult.Message == "Empty inventories")
        {
            await Task.Delay(TimeSpan.FromSeconds(config.DelayInventoryEmptySeconds));
        }
        else
        {
            await Task.Delay(TimeSpan.FromSeconds(config.DelayBetweenAccountsSeconds));
        }
    }

    private void ShowResultsSummary(IReadOnlyCollection<LootResult> lootResults)
    {
        _logger.Information("Statistics");
        _logger.Information($"Items looted: {lootResults.Sum(r => r.LootedItemCount)}");
        _logger.Information($"Successful trades: {lootResults.Count(r => r.Success)}");
        _logger.Information($"Unsuccessful trades: {lootResults.Count(r => !r.Success)}");
    }
}
