using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudCode.Handlers;

static class DeleteModuleHandler
{
    public static async Task DeleteModuleAsync(
        CloudCodeInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync(
            "Deleting module...",
            _ => DeleteModuleAsync(input, unityEnvironment, cloudCodeService, logger, cancellationToken));
    }

    internal static async Task DeleteModuleAsync(
        CloudCodeInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync();
        var projectId = input.CloudProjectId!;

        await cloudCodeService.DeleteModuleAsync(projectId, environmentId, input.ModuleName, cancellationToken);

        logger.LogInformation("Module '{moduleName}' deleted.", input.ModuleName);
    }
}
