using BotLooter.Integrations.Yar.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotLooter.Integrations.Yar.Database;

public class YarContext : DbContext
{
    public DbSet<RewardEntity> Rewards { get; set; }

    public YarContext(DbContextOptions<YarContext> options) : base(options)
    {
    }
}