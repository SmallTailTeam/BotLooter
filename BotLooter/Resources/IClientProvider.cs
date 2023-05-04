using RestSharp;

namespace BotLooter.Resources;

public interface IClientProvider
{
    RestClient Provide();
}