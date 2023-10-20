using System.Text;
using BotLooter;
using BotLooter.Looting;
using BotLooter.Resources;
using BotLooter.Steam;
using Serilog;

var commandLineOptions = CommandLineParser.Parse(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} {Level:w3} : {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

Console.OutputEncoding = Encoding.UTF8;

AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
{
    Log.Logger.Fatal((Exception)eventArgs.ExceptionObject, "Исключение! Для расшифровки можете обратиться к разработчику напрямую или оставить issue на GitHub.");
    
    Console.ReadKey();
};

var version = new Version(0, 3, 4, 0);

var versionChecker = new VersionChecker(Log.Logger);
await versionChecker.Check(version);

var configFilePath = commandLineOptions.ConfigFilePath;
Log.Logger.Information("Используется конфигурационный файл: {ConfigFilePath}", configFilePath);

var configLoadResult = await Configuration.TryLoadFromFile(configFilePath);
if (configLoadResult.Config is not {} config)
{
    FlowUtils.AbortWithError(configLoadResult.Message);
    return;
}

Log.Logger.Information("Инвентари для лута: {Inventories}", string.Join(", ", config.Inventories));

FlowUtils.AskForApproval = config.AskForApproval;
FlowUtils.AskForExit = !config.ExitOnFinish;

var clientProvider = await GetClientProvider();
if (clientProvider is null)
{
    return;
}

CheckThreadCount();

var credentialsLoadResult = await SteamAccountCredentials.TryLoadFromFiles(config);
if (credentialsLoadResult.LootAccounts is not { } accountCredentials)
{
    FlowUtils.AbortWithError(credentialsLoadResult.Message);
    return;
}

FlowUtils.WaitForApproval("Всего аккаунтов для лута: {Count}", accountCredentials.Count);

var lootClients = CreateLootClients();

var looter = new Looter(Log.Logger);

if (!string.IsNullOrWhiteSpace(config.SuccessfulLootsExportFilePath))
{
    var lootResultExporter = new LootResultExporter(config.SuccessfulLootsExportFilePath);
    looter.OnLooted += lootResultExporter.ExportResult;
}

await looter.Loot(lootClients, config.LootTradeOfferUrl, config);
    
FlowUtils.WaitForExit();

async Task<IRestClientProvider?> GetClientProvider()
{
    if (string.IsNullOrWhiteSpace(config.ProxiesFilePath))
    {
        var provider = new LocalRestClientProvider();

        FlowUtils.WaitForApproval("Прокси не указаны, используется локальный клиент.");
        
        return provider;
    }
    else
    {
        var proxyPoolLoadResult = await ProxyRestClientProvider.TryLoadFromFile(config.ProxiesFilePath);

        if (proxyPoolLoadResult.ProxyPool is not { } proxyPool)
        {
            FlowUtils.AbortWithError(proxyPoolLoadResult.Message);
            return null;
        }

        if (proxyPool.AvailableClientsCount == 0)
        {
            FlowUtils.AbortWithError($"В файле '{config.ProxiesFilePath}' отсутствуют прокси");
            return null;
        }
        
        FlowUtils.WaitForApproval("Загружено прокси: {Count}", proxyPool.AvailableClientsCount);

        return proxyPool;
    }
}

void CheckThreadCount()
{
    if (config.LootThreadCount > clientProvider.AvailableClientsCount)
    {
        switch (clientProvider)
        {
            case ProxyRestClientProvider:
                Log.Logger.Warning("Потоков {ThreadCount} больше чем прокси {ClientCount}. Количество потоков будет уменьшено до количества прокси.", config.LootThreadCount, clientProvider.AvailableClientsCount);
                break;
            case LocalRestClientProvider:
                Log.Logger.Warning("Используется локальный клиент, количество потоков будет уменьшено с {ThreadCount} до {ReducedCount}.", config.LootThreadCount, 1);
                break;
        }

        config.LootThreadCount = clientProvider.AvailableClientsCount;
    }
}

List<LootClient> CreateLootClients()
{
    var clients = new List<LootClient>();
    
    foreach (var credentials in accountCredentials)
    {
        var restClient = clientProvider.Provide();
            
        var steamSession = new SteamUserSession(credentials, restClient);
        var steamWeb = new SteamWeb(steamSession);
        var lootClient = new LootClient(credentials, steamSession, steamWeb);
    
        clients.Add(lootClient);
    }

    return clients;
}