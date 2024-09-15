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
            _logger.Warning("Failed to retrieve the latest version of BotLooter.");
            _logger.Information("Your version is {Version}. You can check the latest version here: https://github.com/SmallTailTeam/BotLooter", currentVersion);
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
            _logger.Warning("You are using an outdated version of BotLooter. Version {YourVersion} < {LatestVersion}", currentVersion, releaseVersion);
            _logger.Information("You can download the latest version here: https://github.com/SmallTailTeam/BotLooter");
            return;
        }

        if (currentVersion > releaseVersion)
        {
            _logger.Information("You are likely using a pre-release version of BotLooter. Version {YourVersion} > {LatestVersion}", currentVersion, releaseVersion);
            return;
        }
    }
}