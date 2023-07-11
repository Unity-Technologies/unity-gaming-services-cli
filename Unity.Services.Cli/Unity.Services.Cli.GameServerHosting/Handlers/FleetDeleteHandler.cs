using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Service;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class FleetDeleteHandler
{
    public static async Task FleetDeleteAsync(
        FleetIdInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync("Deleting fleet...",
            _ => FleetDeleteAsync(input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task FleetDeleteAsync(
        FleetIdInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var fleetId = input.FleetId ?? throw new MissingInputException(FleetIdInput.FleetIdKey);

        await service.AuthorizeGameServerHostingService(cancellationToken);

        await service.FleetsApi.DeleteFleetAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            Guid.Parse(fleetId),
            cancellationToken: cancellationToken
        );

        logger.LogInformation("Fleet deleted successfully");
    }
}
