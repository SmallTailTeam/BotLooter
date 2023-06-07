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

    public static async Task<(List<SteamAccountCredentials>? LootAccounts, string Message)> TryLoadFromFiles(Configuration config)
    {
        var loadedAccounts = new List<SteamAccountCredentials>();

        await LoadFromSteamSessions(loadedAccounts, config.SteamSessionsDirectoryPath);
        await LoadFromSecrets(loadedAccounts, config.AccountsFilePath, config.SecretsDirectoryPath);

        return (loadedAccounts, "");
    }

    private static async Task LoadFromSteamSessions(List<SteamAccountCredentials> loadedAccounts, string steamSessionsDirectoryPath)
    {
        if (!Directory.Exists(steamSessionsDirectoryPath))
        {
            Log.Logger.Warning($"Папки с стим-сессиями '{steamSessionsDirectoryPath}' не существует");
            return;
        }

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
                Log.Logger.Warning("Невалидный стим-сессия файл: {FilePath}", filePath);
                continue;
            }

            var accountCredentials = new SteamAccountCredentials(steamSessionFile.Username, steamSessionFile.Password, new SteamGuardAccount
            {
                AccountName = steamSessionFile.Username,
                SharedSecret = steamSessionFile.SharedSecret,
                IdentitySecret = steamSessionFile.IdentitySecret
            });
            
            // TODO: Use the refresh token to obtain web session, somehow
            
            loadedAccounts.Add(accountCredentials);
        }
    }

    private static async Task LoadFromSecrets(List<SteamAccountCredentials> loadedAccounts, string accountsFile, string secretsDirectory)
    {
        if (string.IsNullOrWhiteSpace(accountsFile) || string.IsNullOrWhiteSpace(secretsDirectory))
        {
            return;
        }
        
        if (!File.Exists(accountsFile))
        {
            Log.Logger.Warning($"Файла с аккаунтами '{accountsFile}' не существует");
            return;
        }

        if (!Directory.Exists(secretsDirectory))
        {
            Log.Logger.Warning($"Папки с секретами '{secretsDirectory}' не существует");
            return;
        }
        
        var secrets = await GetSecretFiles(secretsDirectory);

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

            loadedAccounts.Add(new SteamAccountCredentials(login, password, secret));
        }
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
                    Log.Logger.Warning("Невалидный секретный файл: {FilePath}", filePath);
                    continue;
                }

                if (secret is not { SharedSecret: not null, IdentitySecret: not null, DeviceID: not null })
                {
                    Log.Logger.Warning("В секретном файле отсутствует shared_secret, identity_secret или device_id: {Path}", filePath);
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
                Log.Logger.Warning("Невалидный секретный файл: {FilePath}", filePath);
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