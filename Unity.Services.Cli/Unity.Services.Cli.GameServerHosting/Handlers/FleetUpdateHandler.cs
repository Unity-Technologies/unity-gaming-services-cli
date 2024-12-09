using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class FleetUpdateHandler
{
    public static async Task FleetUpdateAsync(FleetUpdateInput input, IUnityEnvironment unityEnvironment,
        IGameServerHostingService service, ILogger logger, ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync($"Updating fleet...",
            _ => FleetUpdateAsync(input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task FleetUpdateAsync(FleetUpdateInput input, IUnityEnvironment unityEnvironment,
        IGameServerHostingService service, ILogger logger, CancellationToken cancellationToken)
    {
        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

        var fleetId = input.FleetId ?? throw new MissingInputException(FleetIdInput.FleetIdKey);
        var allocTtl = input.AllocTtl ?? throw new InvalidCastException();
        var deleteTtl = input.DeleteTtl ?? throw new InvalidCastException();
        var disabledDeleteTtl = input.DisabledDeleteTtl ?? throw new InvalidCastException();
        var shutdownTtl = input.ShutdownTtl ?? throw new InvalidCastException();
        var buildConfigs = input.BuildConfigs;
        var usageSettings = input.UsageSettings;
        var name = input.Name;

        await service.AuthorizeGameServerHostingService(cancellationToken);

        var fleet = await service.FleetsApi.GetFleetAsync(Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId), Guid.Parse(fleetId), cancellationToken: cancellationToken);

        // Build Configs and Name are required parameters. We populate the update request with the previous values
        // If the user does not provide these flags.
        if (buildConfigs?.Count is 0 || name is null)
        {
            name ??= fleet.Name;

            if (buildConfigs?.Count is 0)
            {
                buildConfigs = fleet.BuildConfigurations.ConvertAll(b => b.Id);
            }
        }

        FleetUpdateRequest fleetUpdateReq = new(name: name, buildConfigurations: buildConfigs!, allocationTTL: allocTtl,
            deleteTTL: deleteTtl, disabledDeleteTTL: disabledDeleteTtl, shutdownTTL: shutdownTtl)
        {
            // The API docs for the OsID field mark it as a required, but deprecated field
            // We have a ticket (MPA-1734) to resolve this in future.
#pragma warning disable 612
            OsID = fleet.OsID
#pragma warning restore 612
        };

        // If provided, include usage settings
        if (usageSettings?.Count > 0)
        {
            fleetUpdateReq.UsageSettings = usageSettings.Select(setting => JsonConvert.DeserializeObject<FleetUsageSetting>(setting)!).ToList();
        }
        else if (fleet.UsageSettings?.Count > 0)
        {
            // pass UsageSettings from initial fleet GET request
            fleetUpdateReq.UsageSettings = fleet.UsageSettings;
        }
        else
        {
            // No FleetUsage found. The customer needs to provide at least one for this fleet to be operational.
            throw new CliException("Fleet does not have usage settings. At least 1 fleet usage must exist to be able to scale fleet up. ", ExitCode.HandledError);
        }

        await service.FleetsApi.UpdateFleetAsync(Guid.Parse(input.CloudProjectId!), Guid.Parse(environmentId),
            Guid.Parse(fleetId), fleetUpdateReq, cancellationToken: cancellationToken);

        logger.LogInformation("Fleet updated successfully");
    }
}
