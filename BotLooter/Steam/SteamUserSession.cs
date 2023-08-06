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
    private readonly SteamAccountCredentials _credentials;
    private readonly RestClient _restClient;

    private CookieContainer? _cookieContainer;

    private readonly AsyncRetryPolicy<bool> _acceptConfirmationPolicy;

    public SteamUserSession(SteamAccountCredentials credentials, RestClient restClient)
    {
        _credentials = credentials;
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
        request.AddHeader("sec-ch-ua", @"""Chromium"";v=""104"", "" Not A;Brand"";v=""99"", ""Google Chrome"";v=""104""");
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
        var loginSession = new SteamLoginSession(request => _restClient.ExecuteAsync(request))
        {
            Login = _credentials.Login,
            Password = _credentials.Password,
            SteamGuardCode = _credentials.SteamGuardAccount.GenerateSteamGuardCode(),
            RefreshToken = _credentials.RefreshToken
        };
        
        if (loginSession.RefreshToken is null)
        {
            var loginResult = await loginSession.LoginAsync();

            if (!loginResult.Success)
            {
                return (false, $"Не удалось авторизоваться: {loginResult.Message}");
            }
        }

        _cookieContainer = new CookieContainer();

        var webCookies = await loginSession.GetWebCookies(_cookieContainer);

        if (!webCookies.Success)
        {
            return (false, $"Не удалось получить веб-куки: {webCookies.Message}");
        }

        ulong? steamId = null;

        if (_credentials.SteamId is not null)
        {
            steamId = ulong.Parse(_credentials.SteamId);
        }

        steamId ??= loginSession.SteamId;
        
        if (steamId is null)
        {
            return (false, "Отсутсвует SteamId");
        }

        var sessionId = _cookieContainer.GetAllCookies().FirstOrDefault(c => c.Name == "sessionid")?.Value;
        var steamLoginSecure = _cookieContainer.GetAllCookies().FirstOrDefault(c => c.Name == "steamLoginSecure")?.Value;

        if (sessionId is null || steamLoginSecure is null)
        {
            return (false, "sНе удалось получить веб-куки: (sessionid или steamLoginSecure не найдены)");
        }
        
        _credentials.SteamGuardAccount.Session = new SessionData
        {
            SessionID = sessionId,
            SteamLoginSecure = steamLoginSecure,
            SteamID = steamId.Value
        };

        return (true, "Авторизовался");
    }

    public async ValueTask<bool> AcceptConfirmation(ulong id)
    {
        return await _acceptConfirmationPolicy.ExecuteAsync(async () =>
        {
            // Sometimes FetchConfirmationsAsync throws an exception, we probably don't want to exit the entire program when that happens
            try
            {
                var confirmations = await _credentials.SteamGuardAccount.FetchConfirmationsAsync();

                foreach (var confirmation in confirmations ?? Enumerable.Empty<Confirmation>())
                {
                    if (confirmation.Creator != id)
                    {
                        continue;
                    }

                    var isConfirmed = _credentials.SteamGuardAccount.AcceptConfirmation(confirmation);

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
            request.AddParameter("sessionid", _credentials.SteamGuardAccount.Session.SessionID);
        }
        
        request.CookieContainer = _cookieContainer;

        return await _restClient.ExecuteAsync(request);
    }

    public async ValueTask<RestResponse<T>> WebRequest<T>(RestRequest request, bool withSession = false)
    {
        if (withSession)
        {
            request.AddParameter("sessionid", _credentials.SteamGuardAccount.Session.SessionID);
        }

        request.CookieContainer = _cookieContainer;

        return await _restClient.ExecuteAsync<T>(request);
    }
}