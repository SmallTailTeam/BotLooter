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

var version = new Version(0, 1, 4);

var versionChecker = new VersionChecker(Log.Logger);
await versionChecker.Check(version);

var configLoadResult = await Configuration.TryLoadFromFile();
if (configLoadResult.Config is not {} config)
{
    FlowUtils.AbortWithError(configLoadResult.Message);
    return;
}

Log.Logger.Information("Инвентари для лута: {Inventories}", string.Join(", ", config.Inventories));

FlowUtils.AskForApproval = config.AskForApproval;
FlowUtils.ExitOnFinish = config.ExitOnFinish;

var clientProvider = await GetClientProvider();
if (clientProvider is null)
{
    return;
}

CheckThreadCount();

var credentialsLoadResult = await SteamAccountCredentials.TryLoadFromFiles(config.AccountsFilePath, config.SecretsDirectoryPath);
if (credentialsLoadResult.LootAccounts is not { } accountCredentials)
{
    FlowUtils.AbortWithError(credentialsLoadResult.Message);
    return;
}

FlowUtils.WaitForApproval("Аккаунтов для лута: {Count}", accountCredentials.Count);

var lootClients = CreateLootClients();

var looter = new Looter(Log.Logger);

await looter.Loot(lootClients, config.LootTradeOfferUrl, config);

FlowUtils.WaitForExit();

async Task<IClientProvider?> GetClientProvider()
{
    if (string.IsNullOrWhiteSpace(config.ProxiesFilePath))
    {
        var provider = new LocalClientProvider();

        FlowUtils.WaitForApproval("Прокси не указаны, используется локальный клиент.");
        
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

        if (proxyPool.ClientCount == 0)
        {
            FlowUtils.AbortWithError($"В файле '{config.ProxiesFilePath}' отсутствуют прокси");
            return null;
        }
        
        FlowUtils.WaitForApproval("Загружено прокси: {Count}", proxyPool.ClientCount);

        return proxyPool;
    }
}

void CheckThreadCount()
{
    if (config.LootThreadCount > clientProvider.ClientCount)
    {
        switch (clientProvider)
        {
            case ProxyClientProvider:
                Log.Logger.Warning("Потоков {ThreadCount} > Прокси {ClientCount}. Количество потоков будет уменьшено до количества прокси.", config.LootThreadCount, clientProvider.ClientCount);
                break;
            case LocalClientProvider:
                Log.Logger.Warning("Используется локальный клиент, количество потоков будет уменьшено с {ThreadCount} до {ReducedCount}.", config.LootThreadCount, 1);
                break;
        }

        config.LootThreadCount = clientProvider.ClientCount;
    }
}

List<LootClient> CreateLootClients()
{
    var clients = new List<LootClient>();
    
    foreach (var credentials in accountCredentials)
    {
        var restClient = clientProvider.Provide();
            
        var steamSession = new SteamSession(credentials, restClient);
        var steamWeb = new SteamWeb(steamSession);
        var lootClient = new LootClient(credentials, steamSession, steamWeb);
    
        clients.Add(lootClient);
    }

    return clients;
}