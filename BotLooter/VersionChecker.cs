using Octokit;
using Serilog;

namespace BotLooter;

public class VersionChecker
{
    private readonly ILogger _logger;

    private readonly IGitHubClient _gitHubClient;

    public VersionChecker(ILogger logger)
    {
        _logger = logger;
        
        _gitHubClient = new GitHubClient(new ProductHeaderValue("BotLooter"));
    }

    public async Task Check(Version currentVersion)
    {
        Release? latestRelease = null;

        try
        {
            latestRelease = await _gitHubClient.Repository.Release.GetLatest(635245709);
        }
        catch
        {
            // ignored
        }

        if (latestRelease is null)
        {
            _logger.Warning("Не удалось получить последнюю версию BotLooter.");
            _logger.Information("Ваша версия {Version} Проверить последнюю версию можно здесь https://github.com/SmallTailTeam/BotLooter", currentVersion);
            return;
        }

        if (!Version.TryParse(latestRelease.TagName, out var releaseVersion))
        {
            _logger.Information("BotLooter {Version} https://github.com/SmallTailTeam/BotLooter", currentVersion);
            return;
        }
        
        if (currentVersion == releaseVersion)
        {
            _logger.Information("BotLooter {Version} https://github.com/SmallTailTeam/BotLooter", currentVersion);
            return;
        }

        if (currentVersion < releaseVersion)
        {
            _logger.Warning("Вы используете старую версию BotLooter. Версия {YourVersion} < {LatestVersion}", currentVersion, releaseVersion);
            _logger.Information("Вы можете загрузить последнюю версию здесь https://github.com/SmallTailTeam/BotLooter");
            return;
        }

        if (currentVersion > releaseVersion)
        {
            _logger.Information("Скорее всего вы используете pre-release версию BotLooter. Версия {YourVersion} > {LatestVersion}", currentVersion, releaseVersion);
            return;
        }
    }
}