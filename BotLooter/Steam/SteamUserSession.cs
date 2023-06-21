using System.Net;
using BotLooter.Resources;
using Newtonsoft.Json;
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
    private readonly string _savedSessionsDirectoryPath;

    private CookieContainer? _cookieContainer;

    private readonly AsyncRetryPolicy<bool> _acceptConfirmationPolicy;

    public SteamUserSession(SteamAccountCredentials credentials, RestClient restClient, string savedSessionsDirectoryPath)
    {
        _credentials = credentials;
        _restClient = restClient;
        _savedSessionsDirectoryPath = savedSessionsDirectoryPath;

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
        await TryApplySavedSession();

        _cookieContainer ??= new CookieContainer();

        if (await IsSessionAlive())
        {
            return (true, "Сессия жива");
        }
        
        var loginResult = await TryLogin();
        
        return (loginResult.Success, loginResult.Message);
    }

    private async Task TryApplySavedSession()
    {
        if (string.IsNullOrWhiteSpace(_savedSessionsDirectoryPath))
        {
            return;
        }

        if (_credentials.SteamGuardAccount.Session is not null)
        {
            return;
        }

        var sessionFilePath = Path.Combine(_savedSessionsDirectoryPath, $"{_credentials.Login}.steamweb");

        if (!File.Exists(sessionFilePath))
        {
            return;
        }

        var sessionFileContents = await File.ReadAllTextAsync(sessionFilePath);

        try
        {
            var webCookies = JsonConvert.DeserializeObject<SteamWebCookies>(sessionFileContents);

            if (webCookies is null)
            {
                return;
            }

            _credentials.SteamGuardAccount.Session = new SessionData
            {
                SessionID = webCookies.SessionId,
                SteamLoginSecure = webCookies.SteamLoginSecure,
                SteamID = ulong.Parse(webCookies.SteamId),
                SteamLogin = _credentials.Login
            };
            
            _cookieContainer = CreateCookieContainerWithSession(_credentials.SteamGuardAccount.Session);
        }
        catch
        {
            // ignored
        }
    }

    private async Task TrySaveSession(SteamWebCookies webCookies)
    {
        if (string.IsNullOrWhiteSpace(_savedSessionsDirectoryPath))
        {
            return;
        }

        if (_credentials.SteamGuardAccount.Session is null)
        {
            return;
        }

        Directory.CreateDirectory(_savedSessionsDirectoryPath);

        var savedSession = JsonConvert.SerializeObject(webCookies, Formatting.Indented);
        
        var sessionFilePath = Path.Combine(_savedSessionsDirectoryPath, $"{_credentials.Login}.steamweb");

        await File.WriteAllTextAsync(sessionFilePath, savedSession);
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
        var loginSession = new SteamLoginSession(_restClient)
        {
            Login = _credentials.Login,
            Password = _credentials.Password,
            SteamGuardCode = _credentials.SteamGuardAccount.GenerateSteamGuardCode()
        };

        var loginResult = await loginSession.LoginAsync();

        if (!loginResult.Success)
        {
            return (false, $"Не удалось авторизоваться: {loginResult.Message}");
        }

        var webCookies = await loginSession.GetWebCookies();

        if (webCookies.Cookies is null)
        {
            return (false, $"Не удалось полчить веб-куки: {webCookies.Message}");
        }
        
        _credentials.SteamGuardAccount.Session = new SessionData
        {
            SessionID = webCookies.Cookies.SessionId,
            SteamLoginSecure = webCookies.Cookies.SteamLoginSecure,
            SteamID = ulong.Parse(webCookies.Cookies.SteamId),
            SteamLogin = _credentials.Login
        };

        await TrySaveSession(webCookies.Cookies);
        
        _cookieContainer = CreateCookieContainerWithSession(_credentials.SteamGuardAccount.Session);

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