using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Service;

namespace Unity.Services.Cli.Leaderboards.Handlers;

static class UpdateLeaderboardHandler
{
    public static async Task UpdateLeaderboardAsync(UpdateInput input, IUnityEnvironment unityEnvironment,
        ILeaderboardsService service, ILogger logger, ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Updating leaderboard...",
            context => UpdateAsync(
                input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task UpdateAsync(
        UpdateInput input, IUnityEnvironment unityEnvironment, ILeaderboardsService service,
        ILogger logger, CancellationToken cancellationToken)
    {
        string environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        await service.UpdateLeaderboardAsync(
            input.CloudProjectId!,
            environmentId,
            input.LeaderboardId!,
            await RequestBodyHandler.GetRequestBodyAsync(input.JsonFilePath),
            cancellationToken);

        logger.LogInformation("leaderboard updated!");
    }
}
