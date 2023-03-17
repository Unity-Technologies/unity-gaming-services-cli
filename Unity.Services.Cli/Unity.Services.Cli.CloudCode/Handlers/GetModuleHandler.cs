using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Model;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudCode.Handlers;

static class GetModuleHandler
{
    public static async Task GetModuleAsync(CloudCodeInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync("Fetching module...",
            _ => GetModuleAsync(
                input,
                unityEnvironment,
                cloudCodeService,
                logger,
                cancellationToken
            )
        );
    }

    internal static async Task GetModuleAsync(
        CloudCodeInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync();
        var projectId = input.CloudProjectId!;
        var moduleName = input.ModuleName;

        var module = await cloudCodeService.GetModuleAsync(projectId, environmentId, moduleName!, cancellationToken);
        logger.LogResultValue(new GetModuleResponseOutput(module));
    }
}
