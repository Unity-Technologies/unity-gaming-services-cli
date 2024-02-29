using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class FleetCreateHandler
{
    public static async Task FleetCreateAsync(FleetCreateInput input, IUnityEnvironment unityEnvironment,
        IGameServerHostingService service, ILogger logger, ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync($"Creating fleet...",
            _ => FleetCreateAsync(input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task FleetCreateAsync(FleetCreateInput input, IUnityEnvironment unityEnvironment,
        IGameServerHostingService service, ILogger logger, CancellationToken cancellationToken)
    {
        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var fleetName = input.FleetName ?? throw new MissingInputException(FleetCreateInput.NameKey);
        var osFamily = input.OsFamily ?? throw new MissingInputException(FleetCreateInput.OsFamilyKey);
        var regions = input.Regions ?? throw new MissingInputException(FleetCreateInput.RegionsKey);
        var buildConfigurations = input.BuildConfigurations ?? throw new MissingInputException(FleetCreateInput.BuildConfigurationsKey);

        if (buildConfigurations.Length == 0)
        {
            throw new MissingInputException(FleetCreateInput.BuildConfigurationsKey);
        }

        if (regions.Length == 0)
        {
            throw new MissingInputException(FleetCreateInput.RegionsKey);
        }

        await service.AuthorizeGameServerHostingService(cancellationToken);

        // Parse pre-validated regions, and convert to a region list.
        var regionIdList = regions.Select(Guid.Parse).ToList();
        var regionCreateRequestList = regionIdList.Select(regionId => new Region(regionID: regionId)).ToList();

        var req = new FleetCreateRequest(
            name: fleetName,
            osFamily: osFamily,
            regions: regionCreateRequestList,
            buildConfigurations: buildConfigurations.ToList()
        );

        if (input.UsageSettings != null)
        {
            // Iterate through usage settings array, parse json and save as list. Only if usage settings specified.
            req.UsageSettings = input.UsageSettings.Select(setting => JsonConvert.DeserializeObject<FleetUsageSetting>(setting)!).ToList();
        }

        var fleet = await service.FleetsApi.CreateFleetAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            null,
            fleetCreateRequest: req,
            cancellationToken: cancellationToken
        );

        logger.LogResultValue(new FleetGetOutput(fleet));
    }
}
