using RestSharp;

namespace BotLooter.Resources;

public interface IRestClientProvider
{
    int AvailableClientsCount { get; }
    
    RestClient Provide();
}