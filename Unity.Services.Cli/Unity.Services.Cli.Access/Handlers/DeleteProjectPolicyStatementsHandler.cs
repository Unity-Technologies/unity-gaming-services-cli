using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Access.Input;
using Unity.Services.Cli.Access.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.Access.Handlers;

internal static class DeleteProjectPolicyStatementsHandler
{
    public static async Task DeleteProjectPolicyStatementsAsync(AccessInput input, IUnityEnvironment environment, IAccessService accessService,
        ILogger logger, ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Deleting Project Policy Statements",
            context => DeleteProjectPolicyStatementsAsync(input, environment, accessService, logger, cancellationToken));
    }

    internal static async Task DeleteProjectPolicyStatementsAsync(AccessInput input, IUnityEnvironment unityEnvironment,
        IAccessService accessService, ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync();
        var projectId = input.CloudProjectId!;
        var filePath = input.FilePath!;

        await accessService.DeletePolicyStatementsAsync(projectId, environmentId, filePath, cancellationToken);
        logger.LogInformation("Given policy statements for project: '{projectId}' and environment: '{environmentId}' has been deleted.", projectId, environmentId);
    }
}
