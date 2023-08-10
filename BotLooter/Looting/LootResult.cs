namespace BotLooter.Looting;

public record LootResult(bool Success, string Message, int LootedItemCount = 0);