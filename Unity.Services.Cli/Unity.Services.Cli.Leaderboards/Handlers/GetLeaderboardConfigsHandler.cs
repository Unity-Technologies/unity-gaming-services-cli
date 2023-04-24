using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Newtonsoft.Json;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Model;
using Unity.Services.Cli.Leaderboards.Service;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.Handlers;

static class GetLeaderboardConfigsHandler
{
    public static async Task GetLeaderboardConfigsAsync(
        ListLeaderboardInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching leaderboard list...",
            _ => GetLeaderboardConfigsAsync(input, unityEnvironment, leaderboardsService, logger, cancellationToken));
    }

    internal static async Task  GetLeaderboardConfigsAsync(
        ListLeaderboardInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync();
        var projectId = input.CloudProjectId!;
        var leaderboards = await leaderboardsService.GetLeaderboardsAsync(
            projectId, environmentId, input.Cursor, input.Limit, cancellationToken);
        var result = new GetLeaderboardConfigsResponseOutput(leaderboards);

        logger.LogResultValue(result);
    }
}
