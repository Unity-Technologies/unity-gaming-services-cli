using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Service;

namespace Unity.Services.Cli.Leaderboards.Handlers;

static class PurgePlayerScoresHandler
{
    public static async Task PurgePlayerScoresAsync(
        PurgePlayerInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Purging player scores from all leaderboards...",
            _ => PurgePlayerScoresAsync(input, unityEnvironment, leaderboardsService, logger, cancellationToken));
    }

    internal static async Task PurgePlayerScoresAsync(
        PurgePlayerInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        await leaderboardsService.PurgeLeaderboardPlayerScoresAsync(
            projectId, environmentId, input.PlayerId!, cancellationToken);

        logger.LogResultValue("Player scores purged from all leaderboards.");
    }
}
