using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;

namespace Unity.Services.Cli.GameServerHosting.Services;

class FleetClient : IFleetApi
{
    readonly IFleetsApiAsync m_FleetsApiAsync;
    readonly GameServerHostingApiConfig m_ApiConfig;

    public FleetClient(IFleetsApiAsync fleetsApiAsync, GameServerHostingApiConfig apiConfig)
    {
        m_FleetsApiAsync = fleetsApiAsync;
        m_ApiConfig = apiConfig;
    }

    public async Task<FleetInfo?> FindByName(string name, CancellationToken cancellationToken = default)
    {
        var response = await m_FleetsApiAsync.ListFleetsAsync(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId, cancellationToken: cancellationToken);
        var result = response.FirstOrDefault(
            b => b.Name == name);

        if (result == null)
            return null;

        return new FleetInfo(
            result.Name,
            new FleetId
                { Id = result.Id} ,
            FromApi(result.Status, name),
            #pragma warning disable 0612 //ignore obsolete API warning. We actually need it later.
            result.OsID,
            result.OsName,
            FromApi(result.Regions));
    }

    static FleetInfo.Status FromApi(FleetListItem.StatusEnum statusOption, string fleetName)
    {
        switch (statusOption)
        {
            case FleetListItem.StatusEnum.ONLINE:
                return FleetInfo.Status.Online;
            case FleetListItem.StatusEnum.DRAINING:
                return FleetInfo.Status.Draining;
            case FleetListItem.StatusEnum.OFFLINE:
                return FleetInfo.Status.Offline;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(statusOption),
                    statusOption,
                    $"Unrecognized remote fleet status '{statusOption}' from fleet '{fleetName}'");
        }
    }

    static List<FleetInfo.FleetRegionInfo> FromApi(List<FleetRegion> regions)
    {
        return regions.Select(r => new FleetInfo.FleetRegionInfo(r.RegionID, r.RegionID, r.RegionName)).ToList();
    }

    public async Task<IReadOnlyList<FleetInfo>> List(CancellationToken cancellationToken = new CancellationToken())
    {
        var response = await m_FleetsApiAsync.ListFleetsAsync(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId, cancellationToken: cancellationToken);

        var res = new List<FleetInfo>();

        foreach (var resItem in response)
        {
            res.Add(new FleetInfo(
                resItem.Name,
                id: new FleetId { Id = resItem.Id },
                fleetStatus: FromApi(resItem.Status, resItem.Name),
                osId: resItem.OsID,
                osName: resItem.OsName,
                regions: FromApi(resItem.Regions),
                allocationStatus: FromApi(resItem.Servers)
            ));
        }

        return res;
    }

    static FleetInfo.AllocationStatus FromApi(Servers resItemServers)
    {
        return new FleetInfo.AllocationStatus(
            resItemServers.All.Total,
            resItemServers.All.Status.Allocated,
            resItemServers.All.Status.Available,
            resItemServers.All.Status.Online
        );
    }

    public async Task<FleetInfo> Create(
        string name,
        IList<BuildConfigurationId> buildConfigurations,
        MultiplayConfig.FleetDefinition definition,
        CancellationToken cancellationToken)
    {
        var regions = await GetRegions(cancellationToken);

        var fleet = new FleetCreateRequest(
            name: name,
            buildConfigurations: buildConfigurations.Select(b => b.ToLong()).ToList(),
            regions: definition.Regions.Select(r => new Region(
                regionID: regions[r.Key],
                minAvailableServers: r.Value.MinAvailable,
                maxServers: r.Value.MaxServers)).ToList(),
            osID: Guid.Empty, // Must be set in order to avoid breaking the API
            osFamily: FleetCreateRequest.OsFamilyEnum.LINUX);
        var res = await m_FleetsApiAsync.CreateFleetAsync(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId, fleetCreateRequest: fleet, cancellationToken: cancellationToken);

        return new FleetInfo(
            res.Name,
            new FleetId { Id = res.Id },
            FromApi(res.Status, name),
            res.OsID,
            res.Name,
            FromApi(res.FleetRegions));
    }

    static FleetInfo.Status FromApi(Fleet.StatusEnum statusOption, string fleetName)
    {
        switch (statusOption)
        {
            case Fleet.StatusEnum.ONLINE:
                return FleetInfo.Status.Online;
            case Fleet.StatusEnum.DRAINING:
                return FleetInfo.Status.Draining;
            case Fleet.StatusEnum.OFFLINE:
                return FleetInfo.Status.Offline;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(statusOption),
                    statusOption,
                    $"Unrecognized remote fleet status '{statusOption}' from fleet '{fleetName}'");
        }
    }

    static List<FleetInfo.FleetRegionInfo> FromApi(List<FleetRegion1> regions)
    {
        return regions.Select(r => new FleetInfo.FleetRegionInfo(r.RegionID, r.RegionID, r.RegionName)).ToList();
    }


    public async Task Update(
        FleetId id,
        string name,
        IList<BuildConfigurationId> buildConfigurations,
        MultiplayConfig.FleetDefinition definition,
        Guid osId,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var fleet = new FleetUpdateRequest(
            name: name,
            osID: osId, // Must be set in order to avoid breaking the API
            buildConfigurations: buildConfigurations.Select(b => b.ToLong()).ToList()
        );
        var res = await m_FleetsApiAsync.UpdateFleetAsync(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId, id.ToGuid(), fleet, cancellationToken: cancellationToken);

        await UpdateRegions(id, res, definition, cancellationToken);
    }

    async Task UpdateRegions(FleetId id, Fleet fleet, MultiplayConfig.FleetDefinition definition, CancellationToken cancellationToken = default)
    {
        var regions = await GetRegions(cancellationToken);

        var existingRegions = fleet.FleetRegions.ToDictionary(k => k.RegionName);
        foreach (var (regionName, region) in definition.Regions)
        {
            if (!existingRegions.ContainsKey(regionName))
            {
                var regionDefinition = new AddRegionRequest(
                    regionID: regions[regionName],
                    minAvailableServers: region.MinAvailable,
                    maxServers: region.MaxServers);
                await m_FleetsApiAsync.AddFleetRegionAsync(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId, id.ToGuid(), addRegionRequest: regionDefinition, cancellationToken: cancellationToken);
            }
            else
            {
                var regionId = existingRegions[regionName].RegionID;
                var regionDefinition = new UpdateRegionRequest(
                    scalingEnabled: true,
                    minAvailableServers: region.MinAvailable,
                    maxServers: region.MaxServers);
                await m_FleetsApiAsync
                    .UpdateFleetRegionAsync(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId, id.ToGuid(), regionId, regionDefinition, cancellationToken: cancellationToken);
            }
        }

        foreach (var toRemove in fleet.FleetRegions.Where(r => !definition.Regions.ContainsKey(r.RegionName)))
        {
            await m_FleetsApiAsync.UpdateFleetRegionAsync(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId, id.ToGuid(), toRemove.RegionID, cancellationToken: cancellationToken);
        }
    }

    async Task<Dictionary<string, Guid>> GetRegions(CancellationToken cancellationToken = default)
    {
        var res = await m_FleetsApiAsync.ListTemplateFleetRegionsAsync(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId, cancellationToken: cancellationToken);
        return res.ToDictionary(r => r.Name, r => r.RegionID);
    }

}
