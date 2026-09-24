using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Access.Input;
using Unity.Services.Cli.Access.Models;
using Unity.Services.Cli.Access.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.Access.Handlers;

static class PlayerPolicyListHandler
{
    public static async Task ListPlayerPolicyAsync(PlayerPolicyListInput input, IUnityEnvironment environment, IAccessService accessService,
        ILogger logger, ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Retrieving Player Policy",
            context => ListPlayerPolicyAsync(input, environment, accessService, logger, cancellationToken));
    }

    internal static async Task ListPlayerPolicyAsync(PlayerPolicyListInput input, IUnityEnvironment unityEnvironment,
        IAccessService accessService, ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;

        if (string.IsNullOrEmpty(input.PlayerId))
        {
            var policies = await accessService.GetAllPlayerPoliciesAsync(projectId, environmentId, cancellationToken);
            logger.LogResultValue(new GetAllPlayerPoliciesResponseOutput(policies));
        }
        else
        {
            var policy = await accessService.GetPlayerPolicyAsync(projectId, environmentId, input.PlayerId, cancellationToken);
            logger.LogResultValue(new GetPlayerPolicyResponseOutput(policy));
        }
    }
}
