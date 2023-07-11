using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudCode.Handlers;

static class PublishHandler
{
    public static async Task PublishAsync(
        CloudCodeInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Publishing script...",
            _ => PublishAsync(input, unityEnvironment, cloudCodeService, logger, cancellationToken));
    }

    internal static async Task PublishAsync(
        CloudCodeInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var scriptName = input.ScriptName;
        var version = input.Version ?? 0;
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var result = await cloudCodeService.PublishAsync(
            projectId, environmentId, scriptName!, version, cancellationToken);

        logger.LogInformation(
            "Script version {result._Version} published at {result.DatePublished}.",
            result._Version,
            result.DatePublished);
    }
}
