using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Access.Input;
using Unity.Services.Cli.Access.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.Access.Handlers;

static class PlayerPolicyDeleteHandler
{
    public static async Task DeletePlayerPolicyAsync(PlayerPolicyInput input, IUnityEnvironment environment, IAccessService accessService,
        ILogger logger, ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Deleting Player Policy Statements",
            context => DeletePlayerPolicyAsync(input, environment, accessService, logger, cancellationToken));
    }

    internal static async Task DeletePlayerPolicyAsync(PlayerPolicyInput input, IUnityEnvironment unityEnvironment,
        IAccessService accessService, ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var playerId = input.PlayerId!;
        var statementIds = input.StatementIds!;

        await accessService.DeletePlayerPolicyStatementsAsync(projectId, environmentId, playerId, statementIds, cancellationToken);
        logger.LogInformation("Given policy statements for player: '{playerId}' has been deleted.", playerId);
    }
}
