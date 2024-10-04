namespace BotLooter.Steam.Exceptions;

public class InvalidTradeOfferUrlException : Exception
{
    public string Value { get; }

    public InvalidTradeOfferUrlException(string value)
        => Value = value;

    public override string ToString()
        => $"Invalid trade offer url: {Value}{Environment.NewLine}{base.ToString()}";
}