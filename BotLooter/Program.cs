using System.Text;
using BotLooter;
using BotLooter.Resources;
using BotLooter.Steam;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} {Level:w3} : {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

Console.OutputEncoding = Encoding.UTF8;

AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
{
    Log.Logger.Fatal((Exception)eventArgs.ExceptionObject, "Исключение! Для расшифровки можете обратиться к разработчику напрямую или оставить issue на GitHub.");
    
    Console.ReadKey();
};

var versionChecker = new VersionChecker(Log.Logger);
await versionChecker.Check();

var configLoadResult = await Configuration.TryLoadFromFile();

if (configLoadResult.Config is not {} config)
{
    FlowUtils.AbortWithError(configLoadResult.Message);
    return;
}

FlowUtils.AskForApproval = config.AskForApproval;

var clientProvider = await GetClientProvider();

if (clientProvider is null)
{
    return;
}

var credentialsLoadResult = await SteamAccountCredentials.TryLoadFromFiles(config.AccountsFilePath, config.SecretsDirectoryPath);

if (credentialsLoadResult.LootAccounts is not { } accountCredentials)
{
    FlowUtils.AbortWithError(credentialsLoadResult.Message);
    return;
}

FlowUtils.WaitForApproval("Загружено аккаунтов для лута: {Count}", accountCredentials.Count);

var lootClients = new List<LootClient>();

foreach (var credentials in accountCredentials)
{
    var restClient = clientProvider.Provide();
            
    var steamSession = new SteamSession(credentials, restClient);
    var steamWeb = new SteamWeb(steamSession);
    var lootClient = new LootClient(credentials, steamSession, steamWeb);
    
    lootClients.Add(lootClient);
}

var looter = new Looter(Log.Logger);

await looter.Loot(lootClients, config.LootTradeOfferUrl, config);

FlowUtils.WaitForExit();

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
        
        FlowUtils.WaitForApproval("Загружено прокси: {Count}", proxyPool.ProxyCount);

        return proxyPool;
    }
}