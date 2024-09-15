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
    Log.Logger.Fatal((Exception)eventArgs.ExceptionObject, "Exception! For decoding, you can contact the developer directly or leave an issue on GitHub.");
    
    Console.ReadKey();
};

var version = new Version(0, 3, 8, 0);

var versionChecker = new VersionChecker(Log.Logger);
await versionChecker.Check(version);

Log.Logger.Information("Configuration file: {ConfigFilePath}", commandLineOptions.ConfigFilePath);
var configLoadResult = await Configuration.TryLoadFromFile(commandLineOptions.ConfigFilePath);
if (configLoadResult.Config is not {} config)
{
    FlowUtils.AbortWithError(configLoadResult.Message);
    return;
}

Log.Logger.Information("Inventories for looting: {Inventories}", string.Join(", ", config.Inventories));

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

FlowUtils.WaitForApproval("Total accounts for looting: {Count}", accountCredentials.Count);

var lootClients = CreateLootClients();

var looter = new Looter(Log.Logger);

if (!string.IsNullOrWhiteSpace(config.SuccessfulLootsExportFilePath))
{
    var lootResultExporter = new LootResultExporter(config.SuccessfulLootsExportFilePath);
    looter.OnLooted += lootResultExporter.ExportResult;
}

await looter.Loot(lootClients, config);
    
FlowUtils.WaitForExit();

async Task<IRestClientProvider?> GetClientProvider()
{
    if (string.IsNullOrWhiteSpace(config.ProxiesFilePath))
    {
        var provider = new LocalRestClientProvider();

        FlowUtils.WaitForApproval("Proxies not specified, using local client.");
        
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
            FlowUtils.AbortWithError($"No proxies found in the file '{config.ProxiesFilePath}'");
            return null;
        }
        
        FlowUtils.WaitForApproval("Loaded proxies: {Count}", proxyPool.AvailableClientsCount);

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
                Log.Logger.Warning("Threads {ThreadCount} exceed proxies {ClientCount}. The number of threads will be reduced to the number of proxies.", config.LootThreadCount, clientProvider.AvailableClientsCount);
                break;
            case LocalRestClientProvider:
                Log.Logger.Warning("Using local client, the number of threads will be reduced from {ThreadCount} to {ReducedCount}.", config.LootThreadCount, 1);
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
