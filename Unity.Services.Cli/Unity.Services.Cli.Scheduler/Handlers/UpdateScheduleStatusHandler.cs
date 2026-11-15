using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Scheduler.Input;
using Unity.Services.Cli.Scheduler.Service;
using Unity.Services.Gateway.SchedulerApiV1.Generated.Model;

namespace Unity.Services.Cli.Scheduler.Handlers;

static class UpdateScheduleStatusHandler
{
    public static Task ActivateAsync(
        UpdateScheduleStatusInput input,
        IUnityEnvironment unityEnvironment,
        ISchedulerStatusClient statusClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        return loadingIndicator.StartLoadingAsync(
            "Activating schedule...",
            _ => UpdateStatusAsync(
                input, ScheduleStatusUpdate.Active, unityEnvironment, statusClient, logger, cancellationToken));
    }

    public static Task DeactivateAsync(
        UpdateScheduleStatusInput input,
        IUnityEnvironment unityEnvironment,
        ISchedulerStatusClient statusClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        return loadingIndicator.StartLoadingAsync(
            "Deactivating schedule...",
            _ => UpdateStatusAsync(
                input, ScheduleStatusUpdate.Inactive, unityEnvironment, statusClient, logger, cancellationToken));
    }

    internal static async Task UpdateStatusAsync(
        UpdateScheduleStatusInput input,
        ScheduleStatusUpdate status,
        IUnityEnvironment unityEnvironment,
        ISchedulerStatusClient statusClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        await statusClient.Initialize(environmentId, input.CloudProjectId!, cancellationToken);
        await statusClient.UpdateStatus(input.ScheduleId!, status, cancellationToken);

        logger.LogInformation(
            "schedule {Status}!",
            status == ScheduleStatusUpdate.Active ? "activated" : "deactivated");
    }
}
