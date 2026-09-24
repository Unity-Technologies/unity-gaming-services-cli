using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudCode.Handlers;

static class DeleteScriptVersionHandler
{
    public static async Task DeleteScriptVersionAsync(
        CloudCodeInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync(
            "Deleting script version...",
            _ => DeleteScriptVersionAsync(input, unityEnvironment, cloudCodeService, logger, cancellationToken));
    }

    internal static async Task DeleteScriptVersionAsync(
        CloudCodeInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;

        await cloudCodeService.DeleteScriptVersionAsync(
            projectId,
            environmentId,
            input.ScriptName,
            input.ScriptVersion,
            cancellationToken);

        logger.LogInformation(
            "Version '{version}' of script '{scriptName}' deleted.", input.ScriptVersion, input.ScriptName);
    }
}
