using System.Text.RegularExpressions;

namespace BotLooter.Steam.Contracts;

public partial struct SteamId3
{
    public static readonly SteamId3 Invalid = new (0);

    public ulong Value { get; set; }
    public bool IsValid => Value != 0;

    public SteamId3(ulong value)
    {
        Value = value;
    }
    
    public SteamId3(string value)
    {
        var match = SteamId3Regex().Match(value.ToUpper());

        Value = match.Success ? ulong.Parse(match.Groups[1].Value) : 0UL;
    }
    
    public override string ToString()
        => $"{Value}";
    
    public static implicit operator SteamId3(ulong value)
        => new (value);

    public static implicit operator SteamId3(SteamId64 steamId64)
    {
        if (!steamId64.IsValid) {
            return Invalid;
        }
        
        return steamId64 - SteamId64.Min.Value;
    }
    
    public static implicit operator SteamId64(SteamId3 steamId3)
    {
        return 1L << 56 | 1L << 52 | 1L << 32 | steamId3;
    }

    public static implicit operator ulong(SteamId3 steamId64)
        => steamId64.Value;
    
    public static implicit operator string(SteamId3 steamId64)
        => steamId64.Value.ToString();

    [GeneratedRegex("\\[U:\\d+:(\\d+)\\]")]
    private static partial Regex SteamId3Regex();
}