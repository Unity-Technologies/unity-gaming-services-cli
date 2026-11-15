using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Triggers.Input;
using Unity.Services.Cli.Triggers.Service;
using Unity.Services.Gateway.TriggersApiV1.Generated.Model;

namespace Unity.Services.Cli.Triggers.Handlers;

static class UpdateTriggerStatusHandler
{
    public static Task ActivateAsync(
        UpdateTriggerStatusInput input,
        IUnityEnvironment unityEnvironment,
        ITriggersService triggersService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        return loadingIndicator.StartLoadingAsync(
            "Activating trigger...",
            _ => UpdateStatusAsync(
                input, TriggerStatus.Active, unityEnvironment, triggersService, logger, cancellationToken));
    }

    public static Task DeactivateAsync(
        UpdateTriggerStatusInput input,
        IUnityEnvironment unityEnvironment,
        ITriggersService triggersService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        return loadingIndicator.StartLoadingAsync(
            "Deactivating trigger...",
            _ => UpdateStatusAsync(
                input, TriggerStatus.Inactive, unityEnvironment, triggersService, logger, cancellationToken));
    }

    internal static async Task UpdateStatusAsync(
        UpdateTriggerStatusInput input,
        TriggerStatus status,
        IUnityEnvironment unityEnvironment,
        ITriggersService triggersService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        await triggersService.UpdateTriggerStatusAsync(
            input.CloudProjectId!,
            environmentId,
            input.TriggerId!,
            status,
            cancellationToken);

        logger.LogInformation(
            "trigger {Status}!",
            status == TriggerStatus.Active ? "activated" : "deactivated");
    }
}
