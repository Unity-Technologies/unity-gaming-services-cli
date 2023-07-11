using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudCode.Handlers;

static class DeleteHandler
{
    public static async Task DeleteAsync(
        CloudCodeInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Deleting script...",
            _ => DeleteAsync(input, unityEnvironment, cloudCodeService, logger, cancellationToken));
    }

    internal static async Task DeleteAsync(
        CloudCodeInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        await cloudCodeService.DeleteAsync(projectId, environmentId, input.ScriptName, cancellationToken);

        logger.LogInformation("Script {scriptName} deleted.", input.ScriptName);
    }
}
