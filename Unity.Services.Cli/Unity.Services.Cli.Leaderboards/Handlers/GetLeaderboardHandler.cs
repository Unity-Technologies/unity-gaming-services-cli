using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Model;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Service;

namespace Unity.Services.Cli.Leaderboards.Handlers;

static class GetLeaderboardHandler
{
    public static async Task GetLeaderboardConfigAsync(
        LeaderboardIdInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching leaderboard info...",
            _ => GetLeaderboardConfigAsync(input, unityEnvironment, leaderboardsService, logger, cancellationToken));
    }

    internal static async Task GetLeaderboardConfigAsync(
        LeaderboardIdInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var response = await leaderboardsService.GetLeaderboardAsync(
            projectId, environmentId, input.LeaderboardId!, cancellationToken);

        logger.LogResultValue(new GetLeaderboardResponseOutput(response.Data));
    }
}
