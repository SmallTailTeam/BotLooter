using System.Net;
using BotLooter.Resources;
using Polly;
using Polly.Retry;
using RestSharp;
using SteamAuth;
using SteamSession;

namespace BotLooter.Steam;

public class SteamUserSession
{
    public SteamAccountCredentials Credentials { get; }
    public string? AccessToken { get; private set; }
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
            .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(5));
    }

    public async ValueTask<(bool IsSession, string Message)> TryEnsureSession()
    {
        _cookieContainer ??= new CookieContainer();

        if (await IsSessionAlive())
        {
            return (true, "Сессия жива");
        }
        
        var loginResult = await TryLogin();
        
        return (loginResult.Success, loginResult.Message);
    }

    private async ValueTask<bool> IsSessionAlive()
    {
        if (_cookieContainer?.GetAllCookies().Any(c => c.Name == "steamLoginSecure") == false)
        {
            return false;
        }
        
        var request = new RestRequest("https://store.steampowered.com/account", Method.Head);

        request.AddHeader("Accept", "*/*");
        request.AddHeader("Accept-Encoding", "gzip, deflate, br");
        request.AddHeader("Accept-Language", "en-US,en;q=0.9,ru;q=0.8");
        request.AddHeader("Cache-Control", "no-cache");
        request.AddHeader("Connection", "keep-alive");
        request.AddHeader("DNT", "1");
        request.AddHeader("Pragma", "no-cache");
        request.AddHeader("sec-ch-ua", @"""Chromium"";v=""115"", "" Not A;Brand"";v=""99"", ""Google Chrome"";v=""115""");
        request.AddHeader("sec-ch-ua-mobile", "?0");
        request.AddHeader("sec-ch-ua-platform", @"""Windows""");
        request.AddHeader("Sec-Fetch-Dest", "empty");
        request.AddHeader("Sec-Fetch-Mode", "cors");
        request.AddHeader("Sec-Fetch-Site", "same-origin");
        request.AddHeader("X-Requested-With", "XMLHttpRequest");

        var response = await WebRequest(request);
        
        return response.ResponseUri is not null && !response.ResponseUri.AbsolutePath.StartsWith("/login");
    }

    private async Task<(bool Success, string Message)> TryLogin()
    {
        try
        {
            var loginSession = new SteamLoginSession(request => _restClient.ExecuteAsync(request))
            {
                Login = Credentials.Login,
                Password = Credentials.Password,
                SteamGuardCode = Credentials.SteamGuardAccount.GenerateSteamGuardCode(),
                RefreshToken = Credentials.RefreshToken
            };

            if (loginSession.RefreshToken is null)
            {
                var loginResult = await loginSession.LoginAsync();

                if (!loginResult.Success)
                {
                    return (false, $"Не удалось авторизоваться: {loginResult.Message}");
                }
            }

            var getCookiesResult = await loginSession.GetWebCookies();

            if (getCookiesResult.Cookies is null)
            {
                return (false, $"Не удалось получить веб-куки: {getCookiesResult.Message}");
            }

            AccessToken = getCookiesResult.Cookies.AccessToken;
            SteamId = ulong.Parse(getCookiesResult.Cookies.SteamId);

            Credentials.SteamGuardAccount.Session = new SessionData
            {
                SessionID = getCookiesResult.Cookies.SessionId,
                SteamLoginSecure = getCookiesResult.Cookies.SteamLoginSecure,
                SteamID = SteamId.Value
            };

            _cookieContainer = CreateCookieContainerWithSession(getCookiesResult.Cookies);
            
            return (true, "Авторизовался");
        }
        catch
        {
            var isUsingProxy = _restClient.Options.Proxy is not null;

            return (false,
                $"Не удалось авторизоваться, возможно проблема со Стимом или {(isUsingProxy ? "прокси" : "интернетом")}");
        }
    }

    private CookieContainer CreateCookieContainerWithSession(SteamWebCookies steamCookies)
    {
        const string sessionDomain = "steamcommunity.com";
        const string storeSessionDomain = "store.steampowered.com";
        const string helpSessionDomain = "help.steampowered.com";

        var cookieContainer = new CookieContainer();
        
        cookieContainer.Add(new Cookie("sessionid", steamCookies.SessionId, "/", sessionDomain));
        cookieContainer.Add(new Cookie("steamLoginSecure", steamCookies.SteamLoginSecure, "/", sessionDomain));

        cookieContainer.Add(new Cookie("sessionid", steamCookies.SessionId, "/", storeSessionDomain));
        cookieContainer.Add(new Cookie("steamLoginSecure", steamCookies.SteamLoginSecure, "/", storeSessionDomain));

        cookieContainer.Add(new Cookie("sessionid", steamCookies.SessionId, "/", helpSessionDomain));
        cookieContainer.Add(new Cookie("steamLoginSecure", steamCookies.SteamLoginSecure, "/", helpSessionDomain));
        
        return cookieContainer;
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