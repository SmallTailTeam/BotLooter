using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace BotLooter.Resources;

public class LocalClientProvider : IClientProvider
{
    private readonly RestClient _restClient;

    public LocalClientProvider()
    {
        _restClient = new RestClient(new RestClientOptions
        {
            UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36"
        }, configureSerialization: b => b.UseNewtonsoftJson());
    }

    public RestClient Provide()
    {
        return _restClient;
    }
}