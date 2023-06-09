using BotLooter.Integrations.Yar.Data;
using BotLooter.Integrations.Yar.Database;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BotLooter.Integrations.Yar;

public class RewardWatcher
{
    private readonly YarIntegrationConfiguration _yarConfig;
    private readonly RewardHandlers _rewardHandlers;
    
    private readonly PeriodicTimer _periodicTimer;
    private DateTime _lastCheckTime;

    public RewardWatcher(YarIntegrationConfiguration yarConfig, RewardHandlers rewardHandlers)
    {
        _periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(yarConfig.RewardCheckIntervalSeconds));

        _yarConfig = yarConfig;
        _rewardHandlers = rewardHandlers;
    }

    public void StartWatching()
    {
        _lastCheckTime = DateTime.Now;
        _ = WatchRewards();
    }

    private async Task WatchRewards()
    {
        while (await _periodicTimer.WaitForNextTickAsync())
        {
            try
            {
                await using var db = CreateDbContext();

                var lastDrop = db.Rewards.OrderByDescending(r => r.CreatedAt).FirstOrDefault();

                if (lastDrop is not null)
                {
                    Log.Logger.Debug("Последний дроп: {Date} ({Elapsed} назад) | Дропов всего: {Count}",
                        lastDrop.CreatedAt,
                        DateTime.Now - DateTimeOffset.FromUnixTimeMilliseconds(lastDrop.CreatedAt).DateTime,
                        db.Rewards.Count());
                }

                var newRewards = await db.Rewards
                    .Where(r => r.CreatedAt > ((DateTimeOffset)_lastCheckTime).ToUnixTimeMilliseconds())
                    .ToListAsync();

                _lastCheckTime = DateTime.Now;

                foreach (var reward in newRewards)
                {
                    await _rewardHandlers.Handle(reward);
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Исключение в RewardWatcher");
            }
        }
    }

    private YarContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<YarContext>();
        optionsBuilder.UseSqlite($"Data Source={_yarConfig.DatabaseFilePath}");
        optionsBuilder.UseSnakeCaseNamingConvention();
        
        return new YarContext(optionsBuilder.Options);
    }
}