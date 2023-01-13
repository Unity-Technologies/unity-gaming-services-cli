using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Environment.Input;

namespace Unity.Services.Cli.Environment.Handlers;

static class DeletionHandler
{
    public static async Task DeleteAsync(EnvironmentInput input, IEnvironmentService environmentService,
        ILogger logger, ILoadingIndicator loadingIndicator, IUnityEnvironment unityEnvironment,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Deleting environment...", _ =>
           DeleteAsync(input, environmentService, logger, unityEnvironment, cancellationToken));
    }

    internal static async Task DeleteAsync(EnvironmentInput input, IEnvironmentService environmentService,
        ILogger logger, IUnityEnvironment unityEnvironment, CancellationToken cancellationToken)
    {
        var environmentName = input.EnvironmentName;
        var projectId = input.CloudProjectId;

        var environmentId =
            await unityEnvironment.FetchIdentifierFromSpecificEnvironmentNameAsync(environmentName!);

        await environmentService.DeleteAsync(projectId!, environmentId!, cancellationToken);
        logger.LogInformation("Deleted environment '{environmentName}'", environmentName);
    }
}
