using System.Net;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;

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
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;

        try
        {
            await cloudCodeService.DeleteModuleAsync(projectId, environmentId, input.ModuleName, cancellationToken);
        }
        catch (ApiException ex) when ((HttpStatusCode)ex.ErrorCode == HttpStatusCode.Conflict)
        {
            throw new CliException(
                $"Module '{input.ModuleName}' cannot be deleted because one of its versions is assigned to a"
                + " release. Remove the version from its releases first.", ex, ExitCode.HandledError);
        }

        logger.LogInformation("Module '{moduleName}' deleted.", input.ModuleName);
    }
}
