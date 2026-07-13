using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Observability.Input;
using Unity.Services.Cli.Observability.Model;
using Unity.Services.Cli.Observability.Service;

namespace Unity.Services.Cli.Observability.Handlers;

static class GetLogsHandler
{
    public static async Task GetLogsAsync(
        ListLogsInput input,
        IUnityEnvironment unityEnvironment,
        IObservabilityService observabilityService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching logs...",
            _ => GetLogsAsync(input, unityEnvironment, observabilityService, logger, cancellationToken));
    }

    internal static async Task GetLogsAsync(
        ListLogsInput input,
        IUnityEnvironment unityEnvironment,
        IObservabilityService observabilityService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;

        var response = await observabilityService.GetLogsAsync(
            projectId,
            environmentId,
            input.From,
            input.To,
            input.Query,
            input.Offset,
            input.Limit,
            cancellationToken);

        logger.LogResultValue(new GetLogsResponseOutput(response.Data));
    }
}
