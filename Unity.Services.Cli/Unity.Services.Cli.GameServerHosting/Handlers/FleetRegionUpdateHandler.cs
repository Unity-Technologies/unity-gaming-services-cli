using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
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
        var shutdownTtl = input.ShutdownTtl ?? throw new MissingInputException(FleetRegionUpdateInput.ShutdownTtlKey);

        var maxServers = input.MaxServers;
        var minServers = input.MinAvailableServers;
        var scalingEnabled = input.ScalingEnabled;

        await service.AuthorizeGameServerHostingService(cancellationToken);

        // Fetch the fleet this fleet region belongs to
        var fleet = await service.FleetsApi.GetFleetAsync(Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId), fleetId, cancellationToken: cancellationToken);

        var region = fleet.FleetRegions.Find(r => r.RegionID == regionId);
        if (region == null)
        {
            throw new CliException("Region not found", ExitCode.HandledError);
        }

        if (scalingEnabled != null)
        {
            try
            {
                region.ScalingEnabled = bool.Parse(scalingEnabled);
            }
            catch (FormatException)
            {
                throw new CliException("Invalid --scaling-enabled value, please provide a valid boolean (true|false)", ExitCode.HandledError);
            }
        }

        if (maxServers != null)
        {
            region.MaxServers = (long)maxServers;
            if (scalingEnabled == null)
            {
                if (region.MaxServers > 0)
                {
                    region.ScalingEnabled = true;
                    scalingEnabled = region.ScalingEnabled.ToString();
                }
                else
                {
                    region.ScalingEnabled = false;
                }
            }

        }

        if (minServers != null)
        {
            region.MinAvailableServers = (long)minServers;
            if (scalingEnabled == null)
            {
                region.ScalingEnabled = region.MinAvailableServers > 0;
            }
        }

        var updateFleetRegionResponse = await service.FleetsApi.UpdateFleetRegionAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            fleetId,
            regionId,
            new UpdateRegionRequest(
                deleteTtl,
                disabledDeleteTtl,
                region.MaxServers,
                region.MinAvailableServers,
                region.ScalingEnabled,
                shutdownTtl
            ),
            cancellationToken: cancellationToken
        );

        logger.LogResultValue(new FleetRegionUpdateOutput(updateFleetRegionResponse));
    }
}
