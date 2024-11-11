using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using Serilog;

namespace BotLooter.Resources;

public class ProxyRestClientProvider : IRestClientProvider
{
    public int AvailableClientCount => _proxiedClients.Count;
    
    private readonly List<RestClient> _proxiedClients;
    private int _proxyIndex;

    public ProxyRestClientProvider(List<RestClient> proxiedClients)
    {
        _proxiedClients = proxiedClients;
    }

    public RestClient Provide()
    {
        var proxiedClient = _proxiedClients[_proxyIndex];

        if (++_proxyIndex >= _proxiedClients.Count)
        {
            _proxyIndex = 0;
        }

        return proxiedClient;
    }
    
    public static async Task<(ProxyRestClientProvider? ProxyPool, string Message)> TryLoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return (null, $"Файла с прокси '{filePath}' не существует");
        }

        var lines = await File.ReadAllLinesAsync(filePath);
        
        var proxyConnectionStrings = lines
            .Select(el => new ProxyConnectionString(el.Trim()))
            .Distinct()
            .ToArray();

        var proxiedClients = new List<RestClient>();
        var lineNumber = 0;
        
        foreach (var proxyConnectionString in proxyConnectionStrings)
        {
            lineNumber++;

            var webProxy = proxyConnectionString.TryParse();

            if (webProxy is null)
            {
                Log.Logger.Warning("Неверный формат прокси на строке {LineNumber}", lineNumber);
                continue;
            }

            var restClient = new RestClient(
                o =>
                {
                    o.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";
                    o.FollowRedirects = false;
                    o.MaxTimeout = 60000;
                    o.Proxy = webProxy;
                },
                configureDefaultHeaders: h => 
                {
                    h.Add("Accept", "*/*");
                    h.Add("Connection", "keep-alive");
                },
                configureSerialization: b => b.UseNewtonsoftJson());

            proxiedClients.Add(restClient);
        }

        return (new ProxyRestClientProvider(proxiedClients), "");
    }
}