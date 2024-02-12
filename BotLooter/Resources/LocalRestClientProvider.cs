using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace BotLooter.Resources;

public class LocalRestClientProvider : IRestClientProvider
{
    public int AvailableClientsCount => 1;
    
    private readonly RestClient _restClient;

    public LocalRestClientProvider()
    {
        _restClient = new RestClient(new RestClientOptions
        {
            UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36",
            FollowRedirects = false,
            MaxTimeout = 60000
        }, 
        configureDefaultHeaders: h => 
        {
            h.Add("Accept", "*/*");
            h.Add("Connection", "keep-alive");
        },
        configureSerialization: b => b.UseNewtonsoftJson());
    }

    public RestClient Provide()
    {
        return _restClient;
    }
}