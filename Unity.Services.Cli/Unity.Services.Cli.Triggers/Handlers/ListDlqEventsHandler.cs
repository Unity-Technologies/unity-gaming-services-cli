using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Triggers.Input;
using Unity.Services.Cli.Triggers.Model;
using Unity.Services.Cli.Triggers.Service;

namespace Unity.Services.Cli.Triggers.Handlers;

static class ListDlqEventsHandler
{
    public static async Task ListAsync(
        ListDlqEventsInput input,
        IUnityEnvironment unityEnvironment,
        IDlqService dlqService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching DLQ events...",
            _ => ListAsync(input, unityEnvironment, dlqService, logger, cancellationToken));
    }

    internal static async Task ListAsync(
        ListDlqEventsInput input,
        IUnityEnvironment unityEnvironment,
        IDlqService dlqService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var events = await dlqService.ListDlqEventsAsync(
            projectId, environmentId, input.Limit,
            input.Status, input.CreatedFrom, input.CreatedTo,
            input.ResolutionAction, input.EventId,
            cancellationToken);

        logger.LogResultValue(new ListDlqEventsOutput(events));
    }
}
