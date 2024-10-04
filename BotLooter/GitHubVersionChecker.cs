using Octokit;
using Serilog;

namespace BotLooter;

public class GitHubVersionChecker
{
    private const long RepositoryId = 635245709;
    private const string RepositoryUrl = "https://github.com/SmallTailTeam/BotLooter";
    
    private readonly ILogger _logger;

    private readonly IGitHubClient _gitHubClient;

    public GitHubVersionChecker(ILogger logger)
    {
        _logger = logger;
        
        _gitHubClient = new GitHubClient(new ProductHeaderValue("BotLooter"));
    }

    public async Task Check(Version currentVersion)
    {
        Release? latestRelease = null;

        try
        {
            latestRelease = await _gitHubClient.Repository.Release.GetLatest(RepositoryId);
        }
        catch
        {
            // ignored
        }

        if (latestRelease is null)
        {
            _logger.Warning("Не удалось получить последнюю версию BotLooter.");
            _logger.Information("Ваша версия {Version} Проверить последнюю версию можно здесь {RepositoryUrl}", currentVersion, RepositoryUrl);
            return;
        }

        if (!Version.TryParse(latestRelease.TagName, out var releaseVersion))
        {
            _logger.Information("BotLooter {Version} {RepositoryUrl}", currentVersion, RepositoryUrl);
            return;
        }
        
        if (currentVersion == releaseVersion)
        {
            _logger.Information("BotLooter {Version} {RepositoryUrl}", currentVersion, RepositoryUrl);
            return;
        }

        if (currentVersion < releaseVersion)
        {
            _logger.Warning("Вы используете старую версию BotLooter. Версия {YourVersion} < {LatestVersion}", currentVersion, releaseVersion);
            _logger.Information("Вы можете загрузить последнюю версию здесь {RepositoryUrl}", RepositoryUrl);
            return;
        }

        if (currentVersion > releaseVersion)
        {
            _logger.Information("Скорее всего вы используете pre-release версию BotLooter. Версия {YourVersion} > {LatestVersion}", currentVersion, releaseVersion);
            return;
        }
    }
}