using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Service;

namespace Unity.Services.Cli.Leaderboards.Handlers;

static class CreateLeaderboardHandler
{
    public static async Task CreateLeaderboardAsync(CreateInput input, IUnityEnvironment unityEnvironment,
        ILeaderboardsService service, ILogger logger, ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Creating leaderboard...",
            context => CreateAsync(
                input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task CreateAsync(
        CreateInput input, IUnityEnvironment unityEnvironment, ILeaderboardsService service,
        ILogger logger, CancellationToken cancellationToken)
    {
        string environmentId = await unityEnvironment.FetchIdentifierAsync();
        await service.CreateLeaderboardAsync(
            input.CloudProjectId!,
            environmentId,
            await RequestBodyHandler.GetRequestBodyAsync(input.JsonFilePath),
            cancellationToken);

        logger.LogInformation("leaderboard created!");
    }
}
