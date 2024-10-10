using System.Net;

namespace BotLooter.Resources;

public readonly record struct ProxyConnectionString(string Value)
{
    public static implicit operator string(ProxyConnectionString connectionString)
        => connectionString.Value;
    
    public static implicit operator ProxyConnectionString(string value)
        => new(value);
    
    public WebProxy? TryParse()
    {
        try
        {
            var uri = new Uri(Value);

            var proxy = new WebProxy(uri);

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var credentials = uri.UserInfo.Split(':', 2);

                proxy.Credentials = new NetworkCredential(credentials[0], credentials[1]);
            }

            return proxy;
        }
        catch
        {
            return null;
        }
    }
}