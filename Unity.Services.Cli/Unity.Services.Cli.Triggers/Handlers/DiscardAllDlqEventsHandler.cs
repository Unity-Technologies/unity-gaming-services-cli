using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Triggers.Model;
using Unity.Services.Cli.Triggers.Service;

namespace Unity.Services.Cli.Triggers.Handlers;

static class DiscardAllDlqEventsHandler
{
    public static async Task DiscardAllAsync(
        CommonInput input,
        IUnityEnvironment unityEnvironment,
        IDlqService dlqService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Discarding all pending DLQ events...",
            _ => DiscardAllAsync(input, unityEnvironment, dlqService, logger, cancellationToken));
    }

    internal static async Task DiscardAllAsync(
        CommonInput input,
        IUnityEnvironment unityEnvironment,
        IDlqService dlqService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var result = await dlqService.DiscardAllDlqEventsAsync(
            input.CloudProjectId!, environmentId, cancellationToken);

        logger.LogResultValue(new DiscardAllDlqEventsOutput(result));
    }
}
