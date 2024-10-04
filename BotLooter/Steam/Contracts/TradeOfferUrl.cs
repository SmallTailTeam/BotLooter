using System.Text.RegularExpressions;
using BotLooter.Steam.Exceptions;

namespace BotLooter.Steam.Contracts;

public readonly partial record struct TradeOfferUrl
{
    public string Url { get; }
    
    public SteamId3 Partner { get; }
    public string Token { get; }
    
    public TradeOfferUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidTradeOfferUrlException(url);
        }
        
        var match = TradeOfferUrlRegex().Match(url);
        
        if (!match.Success)
        {
            throw new InvalidTradeOfferUrlException(url);
        }
        
        Url = url;

        Partner = ulong.Parse(match.Groups[1].Value);
        Token = match.Groups[2].Value;
    }

    public override string ToString()
    {
        return Url;
    }

    public static implicit operator TradeOfferUrl(string url)
    {
        return new TradeOfferUrl(url);
    }

    [GeneratedRegex("https?:\\/\\/steamcommunity.com\\/tradeoffer\\/new\\/\\?partner=(\\d+)&token=(.{8})\\/?$", RegexOptions.Compiled)]
    private static partial Regex TradeOfferUrlRegex();
}