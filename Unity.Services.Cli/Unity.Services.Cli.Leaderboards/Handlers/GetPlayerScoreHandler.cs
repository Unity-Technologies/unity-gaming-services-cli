using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Model;
using Unity.Services.Cli.Leaderboards.Service;

namespace Unity.Services.Cli.Leaderboards.Handlers;

static class GetPlayerScoreHandler
{
    public static async Task GetPlayerScoreAsync(
        PlayerScoreInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching player score...",
            _ => GetPlayerScoreAsync(input, unityEnvironment, leaderboardsService, logger, cancellationToken));
    }

    internal static async Task GetPlayerScoreAsync(
        PlayerScoreInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;

        if (input.VersionId != null)
        {
            var response = await leaderboardsService.GetLeaderboardVersionPlayerScoreAsync(
                projectId, environmentId, input.LeaderboardId!, input.VersionId, input.PlayerId!, cancellationToken);
            logger.LogResultValue(new VersionPlayerScoreOutput(response.Data));
        }
        else
        {
            var response = await leaderboardsService.GetLeaderboardPlayerScoreAsync(
                projectId, environmentId, input.LeaderboardId!, input.PlayerId!, cancellationToken);
            logger.LogResultValue(new PlayerScoreOutput(response.Data));
        }
    }
}
