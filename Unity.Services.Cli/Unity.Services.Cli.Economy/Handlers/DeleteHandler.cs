using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Economy.Input;
using Unity.Services.Cli.Economy.Service;

namespace Unity.Services.Cli.Economy.Handlers;

static class DeleteHandler
{
    public static async Task DeleteAsync(EconomyInput input, IUnityEnvironment unityEnvironment, IEconomyService economyService, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Deleting resource...", _ =>
                DeleteAsync(input, unityEnvironment, economyService, logger, cancellationToken));
    }

    internal static async Task DeleteAsync(EconomyInput input, IUnityEnvironment unityEnvironment, IEconomyService economyService,
        ILogger logger, CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

        await economyService.DeleteAsync(input.ResourceId!, projectId, environmentId, cancellationToken);
        logger.LogInformation($"{input.ResourceId} deleted.");
    }
}
