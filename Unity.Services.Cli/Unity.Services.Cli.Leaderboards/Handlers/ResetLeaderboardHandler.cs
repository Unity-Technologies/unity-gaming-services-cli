using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Service;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.Handlers;

static class ResetLeaderboardHandler
{
    public static async Task ResetLeaderboardAsync(ResetInput input, IUnityEnvironment unityEnvironment,
        ILeaderboardsService service, ILogger logger, ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Reseting leaderboard...",
            context => ResetAsync(
                input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task ResetAsync(
        ResetInput input, IUnityEnvironment unityEnvironment, ILeaderboardsService service,
        ILogger logger, CancellationToken cancellationToken)
    {
        string environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var response = await service.ResetLeaderboardAsync(
            input.CloudProjectId!,
            environmentId,
            input.LeaderboardId!,
            input.Archive,
            cancellationToken);
        var message = "leaderboard reset!";
        var version = JsonConvert.DeserializeObject<LeaderboardVersionId>(response.RawContent)!;
        if (!string.IsNullOrWhiteSpace(version.VersionId))
        {
            message = message + " Version Id: " + version.VersionId;
        }

        logger.LogInformation(message);
    }
}
