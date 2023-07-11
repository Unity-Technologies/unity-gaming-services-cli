using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Access.Input;
using Unity.Services.Cli.Access.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.Access.Handlers;

static class UpsertPlayerPolicyHandler
{
    public static async Task UpsertPlayerPolicyAsync(AccessInput input, IUnityEnvironment environment, IAccessService accessService,
        ILogger logger, ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Updating Player Policy",
            context => UpsertPlayerPolicyAsync(input, environment, accessService, logger, cancellationToken));
    }

    internal static async Task UpsertPlayerPolicyAsync(AccessInput input, IUnityEnvironment unityEnvironment,
        IAccessService accessService, ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var filePath = input.FilePath!;
        var playerId = input.PlayerId!;

        await accessService.UpsertPlayerPolicyAsync(projectId, environmentId, playerId, filePath, cancellationToken);
        logger.LogInformation("Policy for player: '{playerId}' has been updated.", playerId);
    }
}
