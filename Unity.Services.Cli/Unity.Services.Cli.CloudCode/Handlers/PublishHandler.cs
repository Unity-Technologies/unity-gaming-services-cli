using System.Net;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

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
        PublishScriptResponse result;
        try
        {
            result = await cloudCodeService.PublishAsync(
                projectId, environmentId, scriptName!, version, cancellationToken);
        }
        catch (ApiException ex) when ((HttpStatusCode)ex.ErrorCode == HttpStatusCode.Conflict)
        {
            throw new CliException(
                $"Script '{scriptName}' cannot be published because it already has the maximum number of"
                + " versions and every older version is assigned to a release. Remove a version from its"
                + " releases first.", ex, ExitCode.HandledError);
        }

        logger.LogInformation(
            "Script version {result._Version} published at {result.DatePublished}.",
            result._Version,
            result.DatePublished);
    }
}
