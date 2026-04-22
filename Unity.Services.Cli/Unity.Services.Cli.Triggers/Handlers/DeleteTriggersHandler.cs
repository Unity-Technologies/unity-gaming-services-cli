using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Triggers.Input;
using Unity.Services.Cli.Triggers.Service;

namespace Unity.Services.Cli.Triggers.Handlers;

static class DeleteTriggersHandler
{
    public static async Task DeleteAsync(
        DeleteTriggerInput input,
        IUnityEnvironment unityEnvironment,
        ITriggersService triggersService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Deleting trigger...",
            _ => DeleteAsync(input, unityEnvironment, triggersService, logger, cancellationToken));
    }

    internal static async Task DeleteAsync(
        DeleteTriggerInput input,
        IUnityEnvironment unityEnvironment,
        ITriggersService triggersService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        await triggersService.DeleteTriggerAsync(
            input.CloudProjectId!,
            environmentId,
            input.TriggerId!,
            cancellationToken);

        logger.LogInformation("trigger deleted!");
    }
}
