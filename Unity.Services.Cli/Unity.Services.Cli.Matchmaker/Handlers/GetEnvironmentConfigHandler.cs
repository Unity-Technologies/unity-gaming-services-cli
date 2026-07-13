using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Matchmaker.Model;
using Unity.Services.Cli.Matchmaker.Service;

namespace Unity.Services.Cli.Matchmaker.Handlers;

static class GetEnvironmentConfigHandler
{
    public static async Task GetEnvironmentConfigAsync(
        CommonInput input,
        IUnityEnvironment unityEnvironment,
        IMatchmakerService matchmakerService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching matchmaker environment config...",
            _ => GetEnvironmentConfigAsync(input, unityEnvironment, matchmakerService, logger, cancellationToken));
    }

    internal static async Task GetEnvironmentConfigAsync(
        CommonInput input,
        IUnityEnvironment unityEnvironment,
        IMatchmakerService matchmakerService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        await matchmakerService.Initialize(projectId, environmentId, cancellationToken);
        var (_, config) = await matchmakerService.GetEnvironmentConfig(cancellationToken);

        logger.LogResultValue(new EnvironmentConfigOutput(config));
    }
}
