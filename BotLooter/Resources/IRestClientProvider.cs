using RestSharp;

namespace BotLooter.Resources;

public interface IRestClientProvider
{
    int AvailableClientCount { get; }
    
    RestClient Provide();
}