using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Economy.Service;

namespace Unity.Services.Cli.Economy.Handlers;

static class PublishHandler
{
    public static async Task PublishAsync(CommonInput input, IUnityEnvironment unityEnvironment, IEconomyService economyService, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Publishing configuration...", _ =>
                PublishAsync(input, unityEnvironment, economyService, logger, cancellationToken));
    }

    internal static async Task PublishAsync(CommonInput input, IUnityEnvironment unityEnvironment, IEconomyService economyService,
        ILogger logger, CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

        await economyService.PublishAsync(projectId, environmentId, cancellationToken);

        logger.LogInformation("Publish successful.");
    }
}
