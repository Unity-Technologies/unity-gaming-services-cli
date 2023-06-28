using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudCode.Model;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudCode.Handlers;

static class ListHandler
{
    public static async Task ListAsync(
        CommonInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching script list...",
            _ => ListAsync(input, unityEnvironment, cloudCodeService, logger, cancellationToken));
    }

    internal static async Task ListAsync(
        CommonInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync();
        var projectId = input.CloudProjectId!;
        var scripts = await cloudCodeService.ListAsync(
            projectId, environmentId, cancellationToken);

        var result = scripts
            .Select(s => new CloudListScriptResult(s.Name, s.LastPublishedDate) )
            .ToList();

        logger.LogResultValue(result);
    }
}
