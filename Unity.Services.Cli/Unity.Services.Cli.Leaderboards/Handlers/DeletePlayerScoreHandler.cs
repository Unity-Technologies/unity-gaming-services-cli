using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Service;

namespace Unity.Services.Cli.Leaderboards.Handlers;

static class DeletePlayerScoreHandler
{
    public static async Task DeletePlayerScoreAsync(
        PlayerScoreInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Deleting player score...",
            _ => DeletePlayerScoreAsync(input, unityEnvironment, leaderboardsService, logger, cancellationToken));
    }

    internal static async Task DeletePlayerScoreAsync(
        PlayerScoreInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        await leaderboardsService.DeleteLeaderboardPlayerScoreAsync(
            projectId, environmentId, input.LeaderboardId!, input.PlayerId!, cancellationToken);

        logger.LogResultValue("Player score deleted successfully.");
    }
}
