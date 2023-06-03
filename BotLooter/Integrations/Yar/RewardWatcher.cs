using BotLooter.Integrations.Yar.Data;
using BotLooter.Integrations.Yar.Database;
using Microsoft.EntityFrameworkCore;

namespace BotLooter.Integrations.Yar;

public class RewardWatcher
{
    private readonly YarIntegrationConfiguration _yarConfig;
    private readonly RewardHandlers _rewardHandlers;
    
    private readonly PeriodicTimer _periodicTimer;
    private DateTime _lastCheckTimeUtc;

    public RewardWatcher(YarIntegrationConfiguration yarConfig, RewardHandlers rewardHandlers)
    {
        _periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(yarConfig.RewardCheckIntervalSeconds));

        _yarConfig = yarConfig;
        _rewardHandlers = rewardHandlers;
    }

    public void StartWatching()
    {
        _lastCheckTimeUtc = DateTime.UtcNow;
        _ = Timer();
    }

    private async Task Timer()
    {
        while (await _periodicTimer.WaitForNextTickAsync())
        {
            await using var db = CreateDbContext();

            var newRewards = await db.Rewards
                .Where(r => r.CreatedAt > _lastCheckTimeUtc)
                .ToListAsync();
            
            _lastCheckTimeUtc = DateTime.UtcNow;
            
            foreach (var reward in newRewards)
            {
                await _rewardHandlers.Handle(reward);
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