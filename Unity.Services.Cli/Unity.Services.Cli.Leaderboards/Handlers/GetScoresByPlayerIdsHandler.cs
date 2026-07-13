using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Model;
using Unity.Services.Cli.Leaderboards.Service;

namespace Unity.Services.Cli.Leaderboards.Handlers;

static class GetScoresByPlayerIdsHandler
{
    public static async Task GetScoresByPlayerIdsAsync(
        PlayerIdsInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching scores by player IDs...",
            _ => GetScoresByPlayerIdsAsync(input, unityEnvironment, leaderboardsService, logger, cancellationToken));
    }

    internal static async Task GetScoresByPlayerIdsAsync(
        PlayerIdsInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var playerIds = input.PlayerIds!.Split(',').Select(id => id.Trim()).ToList();

        if (input.VersionId != null)
        {
            var response = await leaderboardsService.GetLeaderboardVersionScoresByPlayerIdsAsync(
                projectId, environmentId, input.LeaderboardId!, input.VersionId, playerIds, cancellationToken);
            logger.LogResultValue(new VersionScoresByPlayerIdsOutput(response.Data));
        }
        else
        {
            var response = await leaderboardsService.GetLeaderboardScoresByPlayerIdsAsync(
                projectId, environmentId, input.LeaderboardId!, playerIds, cancellationToken);
            logger.LogResultValue(new ScoresByPlayerIdsOutput(response.Data));
        }
    }
}
