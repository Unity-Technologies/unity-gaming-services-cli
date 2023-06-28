using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Access.Input;
using Unity.Services.Cli.Access.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.Access.Handlers;

static class UpsertProjectPolicyHandler
{
    public static async Task UpsertProjectPolicyAsync(AccessInput input, IUnityEnvironment environment, IAccessService accessService,
        ILogger logger, ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Updating Project Policy",
            context => UpsertProjectPolicyAsync(input, environment, accessService, logger, cancellationToken));
    }

    internal static async Task UpsertProjectPolicyAsync(AccessInput input, IUnityEnvironment unityEnvironment,
        IAccessService accessService, ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync();
        var projectId = input.CloudProjectId!;
        var filePath = input.FilePath!;

        await accessService.UpsertPolicyAsync(projectId, environmentId, filePath, cancellationToken);
        logger.LogInformation("Policy for project: '{projectId}' and environment: '{environmentId}' has been updated.", projectId, environmentId);
    }
}
