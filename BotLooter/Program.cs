using System.Text;
using BotLooter;
using BotLooter.Resources;
using BotLooter.Steam;
using RestSharp;

Console.OutputEncoding = Encoding.UTF8;

AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Исключение!");
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

var proxyPoolLoadResult = await ProxyPool.TryLoadFromFile(config.ProxiesFilePath);

if (proxyPoolLoadResult.ProxyPool is not { } proxyPool)
{
    FlowUtils.AbortWithError(proxyPoolLoadResult.Message);
    return;
}

FlowUtils.WaitForApproval($"Загружено прокси: {proxyPool.ProxyCount}");

var credentialsLoadResult = await SteamAccountCredentials.TryLoadFromFiles(config.AccountsFilePath, config.SecretsDirectoryPath);

if (credentialsLoadResult.LootAccounts is not { } credentials)
{
    FlowUtils.AbortWithError(credentialsLoadResult.Message);
    return;
}

FlowUtils.WaitForApproval($"Загружено аккаунтов для лута: {credentials.Count}");

var looter = new Looter();

await looter.Loot(credentials, proxyPool, config.LootTradeOfferUrl, config.DelayBetweenAccountsSeconds);

Console.ReadLine();