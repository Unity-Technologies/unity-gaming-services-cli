using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Model;
using Unity.Services.Cli.Leaderboards.Service;

namespace Unity.Services.Cli.Leaderboards.Handlers;

static class GetBucketScoresHandler
{
    public static async Task GetBucketScoresAsync(
        BucketScoresInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching bucket scores...",
            _ => GetBucketScoresAsync(input, unityEnvironment, leaderboardsService, logger, cancellationToken));
    }

    internal static async Task GetBucketScoresAsync(
        BucketScoresInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var leaderboardId = input.LeaderboardId!;

        if (input.VersionId != null && input.TierId != null)
        {
            var response = await leaderboardsService.GetLeaderboardVersionBucketScoresByTierAsync(
                projectId, environmentId, leaderboardId, input.VersionId, input.BucketId!, input.TierId,
                input.Offset, input.Limit, cancellationToken);
            logger.LogResultValue(new VersionTierScoresOutput(response.Data));
        }
        else if (input.VersionId != null)
        {
            var response = await leaderboardsService.GetLeaderboardVersionBucketScoresAsync(
                projectId, environmentId, leaderboardId, input.VersionId, input.BucketId!,
                input.Offset, input.Limit, cancellationToken);
            logger.LogResultValue(new VersionScoresOutput(response.Data));
        }
        else if (input.TierId != null)
        {
            var response = await leaderboardsService.GetLeaderboardBucketScoresByTierAsync(
                projectId, environmentId, leaderboardId, input.BucketId!, input.TierId,
                input.Offset, input.Limit, cancellationToken);
            logger.LogResultValue(new LeaderboardTierScoresOutput(response.Data));
        }
        else
        {
            var response = await leaderboardsService.GetLeaderboardBucketScoresAsync(
                projectId, environmentId, leaderboardId, input.BucketId!,
                input.Offset, input.Limit, cancellationToken);
            logger.LogResultValue(new LeaderboardScoresOutput(response.Data));
        }
    }
}
