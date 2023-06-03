using System.ComponentModel.DataAnnotations;

namespace BotLooter.Integrations.Yar.Database.Entities;

public class RewardEntity
{
    [Key]
    public int Id { get; set; }
    public string ItemId { get; set; }
    public string ClientId { get; set; }
    public string SteamId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}