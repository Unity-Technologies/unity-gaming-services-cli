using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudCode.Handlers;

static class DeleteModuleVersionHandler
{
    public static async Task DeleteModuleVersionAsync(
        CloudCodeInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync(
            "Deleting module version...",
            _ => DeleteModuleVersionAsync(input, unityEnvironment, cloudCodeService, logger, cancellationToken));
    }

    internal static async Task DeleteModuleVersionAsync(
        CloudCodeInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;

        await cloudCodeService.DeleteModuleVersionAsync(
            projectId,
            environmentId,
            input.ModuleName,
            input.ModuleVersion,
            cancellationToken);

        logger.LogInformation(
            "Version '{version}' of module '{moduleName}' deleted.", input.ModuleVersion, input.ModuleName);
    }
}
