using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Economy.Model;
using Unity.Services.Cli.Economy.Service;

namespace Unity.Services.Cli.Economy.Handlers;

static class GetResourcesHandler
{
    public static async Task GetAsync(CommonInput input, IUnityEnvironment unityEnvironment, IEconomyService economyService, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Fetching resources...", _ =>
                GetAsync(input, unityEnvironment, economyService, logger, cancellationToken));
    }

    internal static async Task GetAsync(CommonInput input, IUnityEnvironment unityEnvironment, IEconomyService economyService,
        ILogger logger, CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

        var response = await economyService.GetResourcesAsync(projectId, environmentId, cancellationToken);
        var result = new EconomyResourcesResponseResult(response);

        logger.LogResultValue(result);
    }
}
