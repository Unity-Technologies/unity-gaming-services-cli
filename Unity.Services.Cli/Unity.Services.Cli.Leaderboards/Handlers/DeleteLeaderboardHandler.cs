using System.Net;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Service;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Client;

namespace Unity.Services.Cli.Leaderboards.Handlers;

static class DeleteLeaderboardHandler
{
    public static async Task DeleteLeaderboardAsync(LeaderboardIdInput input, IUnityEnvironment unityEnvironment,
        ILeaderboardsService service, ILogger logger, ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Deleting leaderboard...",
            context => DeleteAsync(
                input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task DeleteAsync(
        LeaderboardIdInput input, IUnityEnvironment unityEnvironment, ILeaderboardsService service,
        ILogger logger, CancellationToken cancellationToken)
    {
        string environmentId = await unityEnvironment.FetchIdentifierAsync();
        await service.DeleteLeaderboardAsync(
            input.CloudProjectId!,
            environmentId,
            input.LeaderboardId!,
            cancellationToken);

        logger.LogInformation("leaderboard deleted!");
    }
}
