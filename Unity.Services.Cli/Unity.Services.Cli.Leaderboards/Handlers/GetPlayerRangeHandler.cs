using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Model;
using Unity.Services.Cli.Leaderboards.Service;

namespace Unity.Services.Cli.Leaderboards.Handlers;

static class GetPlayerRangeHandler
{
    public static async Task GetPlayerRangeAsync(
        PlayerRangeInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching player score range...",
            _ => GetPlayerRangeAsync(input, unityEnvironment, leaderboardsService, logger, cancellationToken));
    }

    internal static async Task GetPlayerRangeAsync(
        PlayerRangeInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;

        if (input.VersionId != null)
        {
            var response = await leaderboardsService.GetLeaderboardVersionScoresPlayerRangeAsync(
                projectId, environmentId, input.LeaderboardId!, input.VersionId, input.PlayerId!, input.RangeLimit, cancellationToken);
            logger.LogResultValue(new VersionPlayerRangeOutput(response.Data));
        }
        else
        {
            var response = await leaderboardsService.GetLeaderboardScoresPlayerRangeAsync(
                projectId, environmentId, input.LeaderboardId!, input.PlayerId!, input.RangeLimit, cancellationToken);
            logger.LogResultValue(new PlayerRangeOutput(response.Data));
        }
    }
}
