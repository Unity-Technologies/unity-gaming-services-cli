using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Access.Models;
using Unity.Services.Cli.Access.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.Access.Handlers;

internal static class GetProjectPolicyHandler
{
    public static async Task GetProjectPolicyAsync(CommonInput input, IUnityEnvironment environment, IAccessService accessService,
        ILogger logger, ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Retrieving Project Policy",
            context => GetProjectPolicyAsync(input, environment, accessService, logger, cancellationToken));
    }

    internal static async Task GetProjectPolicyAsync(CommonInput input, IUnityEnvironment unityEnvironment,
        IAccessService accessService, ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync();
        var projectId = input.CloudProjectId!;

        var policy = await accessService.GetPolicyAsync(projectId, environmentId, cancellationToken);
        logger.LogResultValue(new GetPolicyResponseOutput(policy));
    }
}
