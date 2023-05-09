using Newtonsoft.Json;
using Serilog;
using SteamAuth;

namespace BotLooter.Resources;

public class SteamAccountCredentials
{
    public string Login { get; set; }
    public string Password { get; set; }
    public SteamGuardAccount SteamGuardAccount { get; set; }

    public SteamAccountCredentials(string login, string password, SteamGuardAccount steamGuardAccount)
    {
        Login = login;
        Password = password;
        SteamGuardAccount = steamGuardAccount;
    }

    public static async Task<(List<SteamAccountCredentials>? LootAccounts, string Message)> TryLoadFromFiles(string accountsFile, string secretsDirectory)
    {
        if (!File.Exists(accountsFile))
        {
            return (null, $"Файла с аккаунтами '{accountsFile}' не существует");
        }

        if (!Directory.Exists(secretsDirectory))
        {
            return (null, $"Папки с секретами '{secretsDirectory}' не существует");
        }
        
        var secrets = await GetSecretFiles(secretsDirectory);
        
        var accounts = new List<SteamAccountCredentials>();
        
        var accountsFileLines = await File.ReadAllLinesAsync(accountsFile);

        var lineNumber = 0;
        
        foreach (var accountLine in accountsFileLines)
        {
            lineNumber++;
            
            var split = accountLine.Split(':');

            if (split.Length != 2)
            {
                Log.Logger.Warning("Неверный формат аккаунта на строке {LineNumber}", lineNumber);
                continue;
            }

            var login = split[0];
            var password = split[1];

            var secret = FindSecretFile(secrets, login);

            if (secret is null)
            {
                Log.Logger.Warning("{Login} - Не найден секретный файл", login);
                continue;
            }

            if (secret is not { SharedSecret: not null, IdentitySecret: not null, DeviceID: not null})
            {
                Log.Logger.Warning("{Login} - В секретном файле отсутствует shared_secret, identity_secret или device_id", login);
                continue;
            }
            
            accounts.Add(new SteamAccountCredentials(login, password, secret));
        }
        
        return (accounts, "");
    }

    private static async Task<List<SteamGuardAccount>> GetSecretFiles(string directoryPath)
    {
        var steamGuardAccounts = new List<SteamGuardAccount>();
            
        var files = Directory.GetFiles(directoryPath);
        
        foreach (var filePath in files)
        {
            try
            {
                var contents = await File.ReadAllTextAsync(filePath);
                    
                var json = JsonConvert.DeserializeObject<SteamGuardAccount>(contents);

                if (json is null)
                {
                    Log.Logger.Warning("Невалидный секретный файл: {FilePath}", filePath);
                    continue;
                }
                
                steamGuardAccounts.Add(json);
            }
            catch
            {
                Log.Logger.Warning("Невалидный секретный файл: {FilePath}", filePath);
            }
        }

        return steamGuardAccounts;
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