using System.Text;
using BotLooter;
using BotLooter.Resources;
using BotLooter.Steam;
using SteamAuth;

var version = new Version(0, 0, 2);

Console.WriteLine($"BotLooter {version} https://github.com/SmallTailTeam/BotLooter");

Console.OutputEncoding = Encoding.UTF8;

AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Исключение! Для расшифровки можете обратиться к разработчику напрямую или оставить issue на GitHub.");
    Console.WriteLine();
    Console.WriteLine(eventArgs.ExceptionObject);
    Console.ReadKey();
};

var configLoadResult = await Configuration.TryLoadFromFile();

if (configLoadResult.Config is not {} config)
{
    FlowUtils.AbortWithError(configLoadResult.Message);
    return;
}

var clientProvider = await GetClientProvider();

if (clientProvider is null)
{
    return;
}

var credentialsLoadResult = await SteamAccountCredentials.TryLoadFromFiles(config.AccountsFilePath, config.SecretsDirectoryPath);

if (credentialsLoadResult.LootAccounts is not { } credentials)
{
    FlowUtils.AbortWithError(credentialsLoadResult.Message);
    return;
}

FlowUtils.WaitForApproval($"Загружено аккаунтов для лута: {credentials.Count}");

var looter = new Looter();

await looter.Loot(credentials, clientProvider, config.LootTradeOfferUrl, config);

Console.ReadLine();

async Task<IClientProvider?> GetClientProvider()
{
    if (string.IsNullOrWhiteSpace(config.ProxiesFilePath))
    {
        var provider = new LocalClientProvider();

        FlowUtils.WaitForApproval("Прокси не указаны, используется локальный клиент");
        
        return provider;
    }
    else
    {
        var proxyPoolLoadResult = await ProxyClientProvider.TryLoadFromFile(config.ProxiesFilePath);

        if (proxyPoolLoadResult.ProxyPool is not { } proxyPool)
        {
            FlowUtils.AbortWithError(proxyPoolLoadResult.Message);
            return null;
        }

        if (proxyPool.ProxyCount == 0)
        {
            FlowUtils.AbortWithError($"В файле '{config.ProxiesFilePath}' отсутствуют прокси");
            return null;
        }
        
        FlowUtils.WaitForApproval($"Загружено прокси: {proxyPool.ProxyCount}");

        return proxyPool;
    }
}