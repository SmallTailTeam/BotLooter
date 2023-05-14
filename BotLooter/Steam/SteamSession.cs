using System.Net;
using BotLooter.Resources;
using Polly;
using Polly.Retry;
using RestSharp;
using SteamAuth;

namespace BotLooter.Steam;

public class SteamSession
{
    private readonly SteamAccountCredentials _credentials;
    private readonly RestClient _restClient;

    private readonly UserLogin _userLogin;
    private CookieContainer? _cookieContainer;

    private readonly AsyncRetryPolicy<bool> _acceptConfirmationPolicy;
    private readonly RetryPolicy<LoginResult> _loginPolicy;

    public SteamSession(SteamAccountCredentials credentials, RestClient restClient)
    {
        _credentials = credentials;
        _restClient = restClient;
        _userLogin = new UserLogin(credentials.Login, credentials.Password);

        if (restClient.Options.Proxy is { } proxy)
        {
            credentials.SteamGuardAccount.Proxy = (WebProxy)proxy;
            _userLogin.Proxy = (WebProxy)proxy;
        }

        _acceptConfirmationPolicy = Policy
            .HandleResult<bool>(x => x is false)
            .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(5));

        _loginPolicy = Policy
            .HandleResult<LoginResult>(x => x != LoginResult.LoginOkay)
            .WaitAndRetry(3, _ => TimeSpan.FromSeconds(30));
    }

    public async ValueTask<(bool IsSession, string Message)> TryEnsureSession()
    {
        _cookieContainer ??= CreateCookieContainerWithSession(_credentials.SteamGuardAccount.Session);

        if (await IsSessionAlive())
        {
            return (true, "Сессия жива");
        }

        if (await TryRefreshSession())
        {
            return (true, "Обновил сессию");
        }

        var loginResult = TryLogin();
        
        return (loginResult.Success, loginResult.Message);
    }
    
    private async ValueTask<bool> IsSessionAlive()
    {
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

    private async ValueTask<bool> TryRefreshSession()
    {
        if (await _credentials.SteamGuardAccount.RefreshSessionAsync())
        {
            _cookieContainer = CreateCookieContainerWithSession(_credentials.SteamGuardAccount.Session);

            var isSessionOkay = await IsSessionAlive();

            return isSessionOkay;
        }

        return false;
    }

    private (bool Success, string Message) TryLogin()
    {
        var loginResult = _loginPolicy.Execute(() =>
        {
            try
            {
                _userLogin.TwoFactorCode = _credentials.SteamGuardAccount.GenerateSteamGuardCode();

                var result = _userLogin.DoLogin();

                return result;
            }
            catch
            {
                return LoginResult.GeneralFailure;
            }
        });
       
        var isLoginOkay = loginResult == LoginResult.LoginOkay;

        if (!isLoginOkay)
        {
            return (false, "Ошибка авторизации: " + loginResult);
        }
        
        _credentials.SteamGuardAccount.Session = _userLogin.Session;
        _cookieContainer = CreateCookieContainerWithSession(_userLogin.Session);

        return (true, "Авторизовался");
    }

    private CookieContainer CreateCookieContainerWithSession(SessionData? sessionData)
    {
        var cookieContainer = new CookieContainer();

        if (sessionData is { SessionID: not null, SteamLoginSecure: not null })
        {
            const string sessionDomain = "steamcommunity.com";
            const string storeSessionDomain = "store.steampowered.com";
            const string helpSessionDomain = "help.steampowered.com";

            cookieContainer.Add(new Cookie("sessionid", sessionData.SessionID, "/", sessionDomain));
            cookieContainer.Add(new Cookie("steamLoginSecure", sessionData.SteamLoginSecure, "/", sessionDomain));

            cookieContainer.Add(new Cookie("sessionid", sessionData.SessionID, "/", storeSessionDomain));
            cookieContainer.Add(new Cookie("steamLoginSecure", sessionData.SteamLoginSecure, "/", storeSessionDomain));

            cookieContainer.Add(new Cookie("sessionid", sessionData.SessionID, "/", helpSessionDomain));
            cookieContainer.Add(new Cookie("steamLoginSecure", sessionData.SteamLoginSecure, "/", helpSessionDomain));
        }

        return cookieContainer;
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