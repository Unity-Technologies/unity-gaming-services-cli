using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Model;
using Unity.Services.Cli.Leaderboards.Service;

namespace Unity.Services.Cli.Leaderboards.Handlers;

static class ListBucketsHandler
{
    public static async Task ListBucketsAsync(
        PaginatedLeaderboardInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching leaderboard buckets...",
            _ => ListBucketsAsync(input, unityEnvironment, leaderboardsService, logger, cancellationToken));
    }

    internal static async Task ListBucketsAsync(
        PaginatedLeaderboardInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;

        if (input.VersionId != null)
        {
            var response = await leaderboardsService.GetLeaderboardVersionBucketsAsync(
                projectId, environmentId, input.LeaderboardId!, input.VersionId, input.Offset, input.Limit, cancellationToken);
            logger.LogResultValue(new VersionBucketsOutput(response.Data));
        }
        else
        {
            var response = await leaderboardsService.GetLeaderboardBucketsAsync(
                projectId, environmentId, input.LeaderboardId!, input.Offset, input.Limit, cancellationToken);
            logger.LogResultValue(new BucketsOutput(response.Data));
        }
    }
}
