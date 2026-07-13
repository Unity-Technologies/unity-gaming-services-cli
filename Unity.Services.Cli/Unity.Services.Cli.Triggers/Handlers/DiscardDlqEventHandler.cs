using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Triggers.Input;
using Unity.Services.Cli.Triggers.Service;

namespace Unity.Services.Cli.Triggers.Handlers;

static class DiscardDlqEventHandler
{
    public static async Task DiscardAsync(
        DlqEventInput input,
        IUnityEnvironment unityEnvironment,
        IDlqService dlqService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Discarding DLQ event...",
            _ => DiscardAsync(input, unityEnvironment, dlqService, logger, cancellationToken));
    }

    internal static async Task DiscardAsync(
        DlqEventInput input,
        IUnityEnvironment unityEnvironment,
        IDlqService dlqService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        await dlqService.DiscardDlqEventAsync(
            input.CloudProjectId!, environmentId, input.EventId!, cancellationToken);

        logger.LogInformation("DLQ event discarded.");
    }
}
