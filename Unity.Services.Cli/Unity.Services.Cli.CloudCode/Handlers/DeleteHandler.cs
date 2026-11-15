using System.Net;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;

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
        try
        {
            await cloudCodeService.DeleteAsync(projectId, environmentId, input.ScriptName, cancellationToken);
        }
        catch (ApiException ex) when ((HttpStatusCode)ex.ErrorCode == HttpStatusCode.Conflict)
        {
            throw new CliException(
                $"Script '{input.ScriptName}' cannot be deleted because one of its versions is assigned to a"
                + " release. Remove the version from its releases first.", ex, ExitCode.HandledError);
        }

        logger.LogInformation("Script {scriptName} deleted.", input.ScriptName);
    }
}
