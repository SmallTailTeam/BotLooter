using System.ComponentModel.DataAnnotations;

namespace BotLooter.Integrations.Yar.Database.Entities;

public class RewardEntity
{
    [Key]
    public int Id { get; set; }
    
    public string ItemId { get; set; }
    public string ClientId { get; set; }
    public string SteamId { get; set; }
    
    [DataType("datetime")]
    public long CreatedAt { get; set; }
    
    [DataType("datetime")]
    public long UpdatedAt { get; set; }
}