namespace BotLooter.Helpers;

public static class StringHelper
{
    public static string RandomString(int length)
    {
        var randomBytes = new byte[length];
        Random.Shared.NextBytes(randomBytes);

        return BitConverter.ToString(randomBytes).Replace("-", "").ToLower();
    }
}