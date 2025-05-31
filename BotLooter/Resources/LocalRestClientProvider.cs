using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace BotLooter.Resources;

public class LocalRestClientProvider : IRestClientProvider
{
    public int AvailableClientCount => 1;
    
    private readonly RestClient _restClient;

    public LocalRestClientProvider()
    {
        _restClient = new RestClient(
            o =>
            {
                o.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";
                o.FollowRedirects = false;
                o.MaxTimeout = (int)TimeSpan.FromSeconds(60).TotalMilliseconds;
            },
            configureDefaultHeaders: h => 
            {
                h.Add("Accept", "application/json, text/plain, */*");
                h.Add("Sec-Fetch-Site", "same-origin");
                h.Add("Sec-Fetch-Mode", "cors");
                h.Add("Sec-Fetch-Dest", "empty");
            },
            configureSerialization: b => b.UseNewtonsoftJson());
    }

    public RestClient Provide()
    {
        return _restClient;
    }
}