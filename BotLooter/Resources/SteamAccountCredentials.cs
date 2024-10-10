using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Serilog;
using SteamAuth;

namespace BotLooter.Resources;

public class SteamAccountCredentials
{
    public string Login { get; set; }
    public string Password { get; set; }
    public string? SteamId { get; set; }
    public string? RefreshToken { get; set; }
    public SteamGuardAccount SteamGuardAccount { get; set; }

    public SteamAccountCredentials(string login, string password, SteamGuardAccount steamGuardAccount)
    {
        Login = login;
        Password = password;
        SteamGuardAccount = steamGuardAccount;
    }

    public static async Task<(List<SteamAccountCredentials>? LootAccounts, string Message)> TryLoadFromFiles(Configuration config)
    {
        var loadedAccounts = new List<SteamAccountCredentials>();

        Log.Logger.Information("Loading accounts...");
        
        var loadedCountFromSteamSessions = await LoadFromSteamSessions(loadedAccounts, config.SteamSessionsDirectoryPath);
        var loadedCountFromSecrets = await LoadFromSecrets(loadedAccounts, config.AccountsFilePath, config.SecretsDirectoryPath);

        loadedAccounts = await FilterLoadedAccounts(config.IgnoreAccountsFilePath, loadedAccounts);
        
        Log.Logger.Information("Steam sessions: {Count}", loadedCountFromSteamSessions);
        Log.Logger.Information("Secrets: {Count}", loadedCountFromSecrets);
        
        return (loadedAccounts, "");
    }

    private static async Task<List<SteamAccountCredentials>> FilterLoadedAccounts(string ignoreAccountsFilePath, List<SteamAccountCredentials> credentials)
    {
        var ignoredLogins = await LoadIgnoredLogins(ignoreAccountsFilePath);

        if (ignoredLogins is null)
        {
            return credentials;
        }
        
        return credentials
            .Where(c => !ignoredLogins.Contains(c.Login))
            .ToList();
    }

    private static async Task<HashSet<string>?> LoadIgnoredLogins(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        if (!File.Exists(filePath))
        {
            Log.Logger.Warning("Ignored logins file '{Path}' not found", filePath);
            return null;
        }

        var lines = await File.ReadAllLinesAsync(filePath);

        return lines
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToHashSet();
    }

    private static async Task<int> LoadFromSteamSessions(List<SteamAccountCredentials> loadedAccounts, string steamSessionsDirectoryPath)
    {
        if (string.IsNullOrWhiteSpace(steamSessionsDirectoryPath))
        {
            return 0;
        }
        
        if (!Directory.Exists(steamSessionsDirectoryPath))
        {
            Log.Logger.Warning($"Directory with steam sessions '{steamSessionsDirectoryPath}' does not exist");
            return 0;
        }

        var loadedCount = 0;

        foreach (var filePath in Directory.GetFiles(steamSessionsDirectoryPath, "*.steamsession"))
        {
            var fileContents = await File.ReadAllTextAsync(filePath);

            SteamSessionFile? steamSessionFile = null;
            
            try
            {
                steamSessionFile = JsonConvert.DeserializeObject<SteamSessionFile>(fileContents);
            }
            catch
            {
                // ignore
            }

            if (steamSessionFile is null)
            {
                Log.Logger.Warning("Invalid steam session file '{FilePath}', only version 2 files are supported", filePath);
                continue;
            }

            var steamGuardAccount = new SteamGuardAccount
            {
                AccountName = steamSessionFile.Username,
                SharedSecret = steamSessionFile.SharedSecret,
                IdentitySecret = steamSessionFile.IdentitySecret,
                DeviceID = GetDeviceId(steamSessionFile.SteamId)
            };
            
            var accountCredentials = new SteamAccountCredentials(steamSessionFile.Username, steamSessionFile.Password, steamGuardAccount)
            {
                SteamId = steamSessionFile.SteamId,
                RefreshToken = steamSessionFile.WebRefreshToken
            };

            loadedAccounts.Add(accountCredentials);
            loadedCount++;
        }

        return loadedCount;
    }
    
    public static string GetDeviceId(string steamId)
    {
        var bytes = Encoding.UTF8.GetBytes(steamId);
        var hashBytes = SHA1.HashData(bytes);
        var hex = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        var formattedHex = Regex.Replace(hex, @"^([0-9a-f]{8})([0-9a-f]{4})([0-9a-f]{4})([0-9a-f]{4})([0-9a-f]{12}).*$", "$1-$2-$3-$4-$5");
        
        return "android:" + formattedHex;
    }

    private static async Task<int> LoadFromSecrets(List<SteamAccountCredentials> loadedAccounts, string accountsFile, string secretsDirectory)
    {
        if (string.IsNullOrWhiteSpace(accountsFile) || string.IsNullOrWhiteSpace(secretsDirectory))
        {
            return 0;
        }
        
        if (!File.Exists(accountsFile))
        {
            Log.Logger.Warning($"Accounts file '{accountsFile}' does not exist");
            return 0;
        }
        
        if (!Directory.Exists(secretsDirectory))
        {
            Log.Logger.Warning($"Secrets directory '{secretsDirectory}' does not exist");
            return 0;
        }

        var loadedCount = 0;
        
        var secrets = await GetSecretFiles(secretsDirectory);

        var accountsFileLines = await File.ReadAllLinesAsync(accountsFile);

        var lineNumber = 0;
        
        foreach (var accountLine in accountsFileLines)
        {
            lineNumber++;
            
            var split = accountLine.Split(':');

            if (split.Length != 2)
            {
                Log.Logger.Warning("Invalid account format on line {LineNumber}", lineNumber);
                continue;
            }

            var login = split[0];
            var password = split[1];

            var secret = FindSecretFile(secrets, login);

            if (secret is null)
            {
                Log.Logger.Warning("{Login} - Secret file not found", login);
                continue;
            }

            loadedAccounts.Add(new SteamAccountCredentials(login, password, secret));
            loadedCount++;
        }

        return loadedCount;
    }

    private static async Task<List<SteamGuardAccount>> GetSecretFiles(string directoryPath)
    {
        var secrets = new List<SteamGuardAccount>();
            
        var files = Directory.GetFiles(directoryPath);
        
        foreach (var filePath in files)
        {
            var contents = await File.ReadAllTextAsync(filePath);
            
            try
            {
                var secret = JsonConvert.DeserializeObject<SteamGuardAccount>(contents);

                if (secret is null)
                {
                    Log.Logger.Warning("Invalid secret file: {FilePath}", filePath);
                    continue;
                }

                if (secret is not { SharedSecret: not null, IdentitySecret: not null })
                {
                    Log.Logger.Warning("Secret file missing shared_secret or identity_secret: {Path}", filePath);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(secret.AccountName))
                {
                    secret.AccountName = Path.GetFileNameWithoutExtension(filePath);
                }
                
                secrets.Add(secret);
            }
            catch
            {
                Log.Logger.Warning("Invalid secret file: {FilePath}", filePath);
            }
        }

        return secrets;
    }

    private static SteamGuardAccount? FindSecretFile(List<SteamGuardAccount> steamGuardAccounts, string login)
    {
        foreach (var steamGuardAccount in steamGuardAccounts)
        {
            var isMatch = steamGuardAccount.AccountName.ToLower() == login.ToLower();

            if (!isMatch)
            {
                continue;
            }

            return steamGuardAccount;
        }

        return null;
    }
}
