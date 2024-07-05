using System.Text.RegularExpressions;

namespace BotLooter.Steam.Contracts;

public readonly partial struct TradeOfferUrl
{
    public string Value { get; }
    public SteamId3? Partner => ToPartner();
    public string? Token => ToToken();
    public SteamId64? SteamId64 => Partner;
    public bool IsValid => _match.Success;

    private readonly Match _match;
    
    public TradeOfferUrl(string value)
    {
        Value = value;

        try
        {
            _match = TradeOfferUrlRegex().Match(Value);
        }
        catch
        {
            _match = Match.Empty;
        }
    }

    private string? ToToken()
    {
        return !IsValid ? null : _match.Groups[2].Value;
    }
    
    private SteamId3? ToPartner()
    {
        return !IsValid ? null : new SteamId3(ulong.Parse(_match.Groups[1].Value));
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator TradeOfferUrl(string url)
    {
        return new TradeOfferUrl(url);
    }

    [GeneratedRegex("https?:\\/\\/steamcommunity.com\\/tradeoffer\\/new\\/\\?partner=(\\d+)&token=(.{8})\\/?$", RegexOptions.Compiled)]
    private static partial Regex TradeOfferUrlRegex();
}