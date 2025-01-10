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
        _logger.Information("Начинаю лутать. Потоков: {ThreadCount}", config.LootThreadCount);

        var lootReceiverPicker = new LootReceiverPicker(config);
        
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

                var lootReceiver = lootReceiverPicker.Pick();

                var lootResult = await lootClient.TryLoot(lootReceiver, config);

                lootResults.Add(lootResult);

                Interlocked.Add(ref summaryLootedItems, lootResult.LootedItemCount);

                OnLooted?.Invoke(lootClient.Credentials.Login, lootResult);

                var progress = $"{Interlocked.Increment(ref counter)}/{lootClients.Count}";

                var identifier = $"{lootClient.Credentials.Login}";

                _logger.Information($"{progress} | {identifier} | {lootReceiver.Partner} | {lootResult.Message}");

                await WaitForNextLoot(lootResult, config);
            });
        
        _logger.Information("Лутание завершено");
        
        ShowResultsSummary(lootResults);
    }

    private async Task WaitForNextLoot(LootResult lootResult, Configuration config)
    {
        if (lootResult.Message == "Пустые инвентари")
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
        _logger.Information("Статистика");
        _logger.Information($"Предметов залутано: {lootResults.Sum(r => r.LootedItemCount)}");
        _logger.Information($"Успешных обменов: {lootResults.Count(r => r.Success)}");
        _logger.Information($"Неуспешных обменов: {lootResults.Count(r => !r.Success)}");
    }
}