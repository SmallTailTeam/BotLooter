using System.Collections.Concurrent;
using BotLooter.Resources;
using BotLooter.Steam.Contracts;
using Serilog;

namespace BotLooter.Looting;

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

        var lootResults = new ConcurrentBag<LootResult>();
        
        var counter = 0;

        await Parallel.ForEachAsync(lootClients, new ParallelOptions
        {
            MaxDegreeOfParallelism = config.LootThreadCount
        }, async (lootClient, _) =>
        {
            var lootResult = await lootClient.TryLoot(tradeOfferUrl, config);
            
            lootResults.Add(lootResult);

            var progress = $"{Interlocked.Increment(ref counter)}/{lootClients.Count}";
            
            var identifier = $"{lootClient.Credentials.Login}";
            
            _logger.Information($"{progress} | {identifier} | {lootResult.Message}");

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