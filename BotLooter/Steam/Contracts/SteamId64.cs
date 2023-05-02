namespace BotLooter.Steam.Contracts;

public struct SteamId64
{
    public static readonly SteamId64 Max = new (0x01100001FFFFFFFF);
    public static readonly SteamId64 Min = new (0x110000100000000);
    
    public ulong Value { get; set; }
    public bool IsValid => Value >= Min && Value <= Max;

    public SteamId64(ulong value)
    {
        Value = value;
    }

    public override string ToString()
        => $"{Value}";

    public static implicit operator SteamId64(ulong value)
        => new (value);

    public static implicit operator ulong(SteamId64 steamId64)
        => steamId64.Value;
    
    public static implicit operator string(SteamId64 steamId64)
        => steamId64.Value.ToString();
}