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

static class FleetRegionUpdateHandler
{
    public static async Task FleetRegionUpdateAsync(
        FleetRegionUpdateInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync($"Updating fleet region...",
            _ => FleetRegionUpdateAsync(input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task FleetRegionUpdateAsync(
        FleetRegionUpdateInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var fleetId = input.FleetId ?? throw new MissingInputException(FleetRegionUpdateInput.FleetIdKey);
        var regionId = input.RegionId ?? throw new MissingInputException(FleetRegionUpdateInput.RegionIdKey);
        var deleteTtl = input.DeleteTtl ?? throw new MissingInputException(FleetRegionUpdateInput.DeleteTtlKey);
        var disabledDeleteTtl = input.DisabledDeleteTtl ?? throw new MissingInputException(FleetRegionUpdateInput.DisabledDeleteTtlKey);
        var maxServers = input.MaxServers ?? throw new MissingInputException(FleetRegionUpdateInput.MaxServersKey);
        var minAvailableServers = input.MinAvailableServers ?? throw new MissingInputException(FleetRegionUpdateInput.MinAvailableServersKey);
        var scalingEnabled = input.ScalingEnabled ?? throw new MissingInputException(FleetRegionUpdateInput.ScalingEnabledKey);
        var shutdownTtl = input.ShutdownTtl ?? throw new MissingInputException(FleetRegionUpdateInput.ShutdownTtlKey);

        await service.AuthorizeGameServerHostingService(cancellationToken);

        var updateFleetRegionResponse = await service.FleetsApi.UpdateFleetRegionAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            fleetId,
            regionId,
            new UpdateRegionRequest(
                deleteTtl,
                disabledDeleteTtl,
                maxServers,
                minAvailableServers,
                scalingEnabled,
                shutdownTtl
            ),
            cancellationToken: cancellationToken
        );

        logger.LogResultValue(new FleetRegionUpdateOutput(updateFleetRegionResponse));
    }
}
