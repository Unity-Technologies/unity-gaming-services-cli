using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Matchmaker.Model;
using Unity.Services.Cli.Matchmaker.Service;

namespace Unity.Services.Cli.Matchmaker.Handlers;

static class GetRestrictionsHandler
{
    public static async Task GetRestrictionsAsync(
        CommonInput input,
        IUnityEnvironment unityEnvironment,
        IMatchmakerService matchmakerService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching matchmaker restrictions...",
            _ => GetRestrictionsAsync(input, unityEnvironment, matchmakerService, logger, cancellationToken));
    }

    internal static async Task GetRestrictionsAsync(
        CommonInput input,
        IUnityEnvironment unityEnvironment,
        IMatchmakerService matchmakerService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        await matchmakerService.Initialize(projectId, environmentId, cancellationToken);
        var restrictions = await matchmakerService.GetRestrictions(cancellationToken);

        logger.LogResultValue(new RestrictionsOutput(restrictions));
    }
}
