using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Access.Input;
using Unity.Services.Cli.Access.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.Access.Handlers;

internal static class DeletePlayerPolicyStatementsHandler
{
    public static async Task DeletePlayerPolicyStatementsAsync(AccessInput input, IUnityEnvironment environment, IAccessService accessService,
        ILogger logger, ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Deleting Player Policy Statements",
            context => DeletePlayerPolicyStatementsAsync(input, environment, accessService, logger, cancellationToken));
    }

    internal static async Task DeletePlayerPolicyStatementsAsync(AccessInput input, IUnityEnvironment unityEnvironment,
        IAccessService accessService, ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync();
        var projectId = input.CloudProjectId!;
        var playerId = input.PlayerId!;
        var filePath = input.FilePath!;

        await accessService.DeletePlayerPolicyStatementsAsync(projectId, environmentId, playerId, filePath, cancellationToken);
        logger.LogInformation("Given policy statements for player: '{playerId}' has been deleted.", playerId);
    }
}
