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
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Safari/537.36",
            FollowRedirects = false
        }, configureSerialization: b => b.UseNewtonsoftJson());
    }

    public RestClient Provide()
    {
        return _restClient;
    }
}