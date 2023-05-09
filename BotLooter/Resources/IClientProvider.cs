using RestSharp;

namespace BotLooter.Resources;

public interface IClientProvider
{
    int ClientCount { get; }
    
    RestClient Provide();
}