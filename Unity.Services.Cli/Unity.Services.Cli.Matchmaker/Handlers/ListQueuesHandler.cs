using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Matchmaker.Model;
using Unity.Services.Cli.Matchmaker.Service;

namespace Unity.Services.Cli.Matchmaker.Handlers;

static class ListQueuesHandler
{
    public static async Task ListQueuesAsync(
        CommonInput input,
        IUnityEnvironment unityEnvironment,
        IMatchmakerService matchmakerService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching matchmaker queues...",
            _ => ListQueuesAsync(input, unityEnvironment, matchmakerService, logger, cancellationToken));
    }

    internal static async Task ListQueuesAsync(
        CommonInput input,
        IUnityEnvironment unityEnvironment,
        IMatchmakerService matchmakerService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        await matchmakerService.Initialize(projectId, environmentId, cancellationToken);
        var queues = await matchmakerService.ListQueues(cancellationToken);

        logger.LogResultValue(new QueueListOutput(queues));
    }
}
