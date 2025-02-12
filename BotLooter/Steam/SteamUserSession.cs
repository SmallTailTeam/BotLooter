using System.Net;
using BotLooter.Resources;
using Polly;
using Polly.Retry;
using RestSharp;
using SmallTail.SteamSession;
using SteamAuth;
using static BotLooter.Resources.SteamAccountCredentials;

namespace BotLooter.Steam;

public class SteamUserSession
{
    public SteamAccountCredentials Credentials { get; }
    public ulong? SteamId { get; private set; }
    
    private readonly RestClient _restClient;

    private CookieContainer? _cookieContainer;

    private readonly AsyncRetryPolicy<bool> _acceptConfirmationPolicy;

    public SteamUserSession(SteamAccountCredentials credentials, RestClient restClient)
    {
        Credentials = credentials;
        _restClient = restClient;

        if (restClient.Options.Proxy is { } proxy)  
        {
            credentials.SteamGuardAccount.Proxy = proxy;
        }

        _acceptConfirmationPolicy = Policy
            .HandleResult<bool>(x => x is false)
            .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(10));
    }

    public async ValueTask<(bool IsSession, string Message)> TryEnsureSession()
    {
        if (await IsSessionAlive())
        {
            return (true, "Сессия жива");
        }
        
        var loginResult = await TryLogin();
        
        return (loginResult.Success, loginResult.Message);
    }

    private async ValueTask<bool> IsSessionAlive()
    {
        if (_cookieContainer is null)
        {
            return false;
        }
        
        var request = new RestRequest("https://store.steampowered.com/account", Method.Head);

        var response = await WebRequest(request);
        
        return response.ResponseUri is not null && !response.ResponseUri.AbsolutePath.StartsWith("/login");
    }

    private async Task<(bool Success, string Message)> TryLogin()
    {
        try
        {
            var loginSession = new SteamLoginSession(_restClient)
            {
                Login = Credentials.Login,
                Password = Credentials.Password,
                RefreshToken = Credentials.RefreshToken
            };

            if (loginSession.RefreshToken is null)
            {
                loginSession.SteamGuardCode = Credentials.SteamGuardAccount.GenerateSteamGuardCode();
                
                var loginResult = await loginSession.LoginAsync();

                if (!loginResult.Success)
                {
                    return (false, $"Не удалось авторизоваться: {loginResult.Message} {(loginResult.XEResult is null ? "" : $"({loginResult.XEResult})")}");
                }
            }

            _cookieContainer ??= new CookieContainer();

            var getCookiesResult = await loginSession.GetWebCookies(_cookieContainer);

            if (!getCookiesResult.Success)
            {
                return (false, $"Не удалось получить веб-куки: {getCookiesResult.Message}");
            }

            SteamId = ulong.Parse(getCookiesResult.SteamId!);

            var cookies = _cookieContainer.GetCookies(new Uri("https://steamcommunity.com"));

            var sessionId = cookies.FirstOrDefault(c => c.Name == "sessionid")?.Value;
            var steamLoginSecure = cookies.FirstOrDefault(c => c.Name == "steamLoginSecure")?.Value;

            Credentials.SteamGuardAccount.Session = new SessionData
            {
                SessionID = sessionId,
                SteamLoginSecure = steamLoginSecure,
                SteamID = SteamId.Value
            };

            if (Credentials.SteamGuardAccount.DeviceID is null)
            {
                Credentials.SteamGuardAccount.DeviceID = GetDeviceId(SteamId.Value.ToString());
            }
            
            return (true, "Авторизовался");
        }
        catch
        {
            var isUsingProxy = _restClient.Options.Proxy is not null;

            return (false,
                $"Не удалось авторизоваться, возможно проблема со Стимом или {(isUsingProxy ? "прокси" : "интернетом")}");
        }
    }
    
    public async ValueTask<bool> AcceptConfirmation(ulong id)
    {
        return await _acceptConfirmationPolicy.ExecuteAsync(async () =>
        {
            // Sometimes FetchConfirmationsAsync throws an exception, we probably don't want to exit the entire program when that happens
            try
            {
                var confirmations = await Credentials.SteamGuardAccount.FetchConfirmationsAsync();

                foreach (var confirmation in confirmations ?? Enumerable.Empty<Confirmation>())
                {
                    if (confirmation.Creator != id)
                    {
                        continue;
                    }

                    var isConfirmed = Credentials.SteamGuardAccount.AcceptConfirmation(confirmation);

                    return isConfirmed;
                }

                return false;
            }
            catch
            {
                return false;
            }
        });
    }
    
    public async ValueTask<RestResponse> WebRequest(RestRequest request, bool withSession = false)
    {
        if (withSession)
        {
            request.AddParameter("sessionid", Credentials.SteamGuardAccount.Session.SessionID);
        }
        
        request.CookieContainer = _cookieContainer;

        return await _restClient.ExecuteAsync(request);
    }

    public async ValueTask<RestResponse<T>> WebRequest<T>(RestRequest request, bool withSession = false)
    {
        if (withSession)
        {
            request.AddParameter("sessionid", Credentials.SteamGuardAccount.Session.SessionID);
        }

        request.CookieContainer = _cookieContainer;

        return await _restClient.ExecuteAsync<T>(request);
    }
}