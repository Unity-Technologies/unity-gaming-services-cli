using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Access.Input;
using Unity.Services.Cli.Access.Models;
using Unity.Services.Cli.Access.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.Access.Handlers;

static class GetPlayerPolicyHandler
{
    public static async Task GetPlayerPolicyAsync(AccessInput input, IUnityEnvironment environment, IAccessService accessService,
        ILogger logger, ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Retrieving Player Policy",
            context => GetPlayerPolicyAsync(input, environment, accessService, logger, cancellationToken));
    }

    internal static async Task GetPlayerPolicyAsync(AccessInput input, IUnityEnvironment unityEnvironment,
        IAccessService accessService, ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync();
        var projectId = input.CloudProjectId!;
        var playerId = input.PlayerId!;

        var policy = await accessService.GetPlayerPolicyAsync(projectId, environmentId, playerId, cancellationToken);
        logger.LogResultValue(new GetPlayerPolicyResponseOutput(policy));
    }
}
