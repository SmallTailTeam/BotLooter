using Newtonsoft.Json;
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

        var accounts = new List<SteamAccountCredentials>();
        
        var accountsFileLines = await File.ReadAllLinesAsync(accountsFile);

        var lineNumber = 0;
        
        foreach (var accountLine in accountsFileLines)
        {
            lineNumber++;
            
            var split = accountLine.Split(':');

            if (split.Length != 2)
            {
                Console.WriteLine($"Неверный формат аккаунта, номер строки: {lineNumber}, строка: '{accountLine}'");
                continue;
            }

            var login = split[0];
            var password = split[1];

            var secretFilePath = Path.Combine(secretsDirectory, login + ".maFile");

            if (!File.Exists(secretFilePath))
            {
                Console.WriteLine($"{login}: Не найден секретный файл для аккаунта");
                continue;
            }

            var secretFileContents = await File.ReadAllTextAsync(secretFilePath);

            SteamGuardAccount? steamGuardAccount;

            try
            {
                steamGuardAccount = JsonConvert.DeserializeObject<SteamGuardAccount>(secretFileContents);
            }
            catch
            {
                Console.WriteLine($"{login}: Не удалось десериализовать секретный файл");
                continue;
            }

            if (steamGuardAccount is not { SharedSecret: not null, IdentitySecret: not null})
            {
                Console.WriteLine($"{login}: В секретном файле отсутствует SharedSecret или IdentitySecret");
                continue;
            }
            
            accounts.Add(new SteamAccountCredentials(login, password, steamGuardAccount));
        }
        
        return (accounts, "");
    }
}