using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Model;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudCode.Handlers;

static class GetModuleSpecHandler
{
    public static async Task GetModuleSpecAsync(CloudCodeInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync("Fetching module spec...",
            _ => GetModuleSpecAsync(
                input,
                unityEnvironment,
                cloudCodeService,
                logger,
                cancellationToken
            )
        );
    }

    internal static async Task GetModuleSpecAsync(
        CloudCodeInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var moduleName = input.ModuleName;

        var spec = await cloudCodeService.GetModuleSpecAsync(
            projectId,
            environmentId,
            moduleName!,
            input.ModuleSpecVersion,
            cancellationToken);
        logger.LogResultValue(new ModuleSpecOutput(spec));
    }
}
