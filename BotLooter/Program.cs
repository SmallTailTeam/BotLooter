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
    Log.Logger.Fatal((Exception)eventArgs.ExceptionObject, "Exception! You can contact the developer directly for decryption or leave an issue on GitHub.");
    
    Console.ReadKey();
};

var version = new Version(0, 3, 8, 0);

var versionChecker = new GitHubVersionChecker(Log.Logger);
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

        FlowUtils.WaitForApproval("No proxies specified, using local client.");
        
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

        if (proxyPool.AvailableClientCount == 0)
        {
            FlowUtils.AbortWithError($"No proxies found in file '{config.ProxiesFilePath}'");
            return null;
        }
        
        FlowUtils.WaitForApproval("Loaded proxies: {Count}", proxyPool.AvailableClientCount);

        return proxyPool;
    }
}

void CheckThreadCount()
{
    if (config.LootThreadCount > clientProvider.AvailableClientCount)
    {
        switch (clientProvider)
        {
            case ProxyRestClientProvider:
                Log.Logger.Warning("Threads {ThreadCount} exceed proxies {ClientCount}. Thread count will be reduced to proxy count.", config.LootThreadCount, clientProvider.AvailableClientCount);
                break;
            case LocalRestClientProvider:
                Log.Logger.Warning("Using local client, thread count will be reduced from {ThreadCount} to {ReducedCount}.", config.LootThreadCount, 1);
                break;
        }

        config.LootThreadCount = clientProvider.AvailableClientCount;
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
