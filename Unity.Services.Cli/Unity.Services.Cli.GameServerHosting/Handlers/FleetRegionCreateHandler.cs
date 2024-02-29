using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class FleetRegionCreateHandler
{
    public static async Task FleetRegionCreateAsync(
        FleetRegionCreateInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync("Creating fleet region...",
            _ => FleetRegionCreateAsync(input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task FleetRegionCreateAsync(
        FleetRegionCreateInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var fleetId = input.FleetId ?? throw new MissingInputException(FleetRegionCreateInput.FleetIdKey);
        var regionId = input.RegionId ?? throw new MissingInputException(FleetRegionCreateInput.RegionIdKey);
        var maxServers = input.MaxServers ?? throw new MissingInputException(FleetRegionCreateInput.MaxServersKey);
        var minAvailableServers = input.MinAvailableServers ?? throw new MissingInputException(FleetRegionCreateInput.MinAvailableServersKey);

        await service.AuthorizeGameServerHostingService(cancellationToken);

        var addFleetRegionsResponse = await service.FleetsApi.AddFleetRegionAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            fleetId,
            addRegionRequest: new AddRegionRequest(
                maxServers,
                minAvailableServers,
                regionId
            ),
            cancellationToken: cancellationToken
        );

        logger.LogResultValue(new FleetRegionCreateOutput(addFleetRegionsResponse));
    }
}
