using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Access.Models;
using Unity.Services.Cli.Access.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.Access.Handlers;

static class GetAllPlayerPoliciesHandler
{
    public static async Task GetAllPlayerPoliciesAsync(CommonInput input, IUnityEnvironment environment, IAccessService accessService,
        ILogger logger, ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Retrieving Player Policies",
            context => GetAllPlayerPoliciesAsync(input, environment, accessService, logger, cancellationToken));
    }

    internal static async Task GetAllPlayerPoliciesAsync(CommonInput input, IUnityEnvironment unityEnvironment,
        IAccessService accessService, ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;

        var playerPolicies = await accessService.GetAllPlayerPoliciesAsync(projectId, environmentId, cancellationToken);

        logger.LogResultValue(new GetAllPlayerPoliciesResponseOutput(playerPolicies));
    }
}
