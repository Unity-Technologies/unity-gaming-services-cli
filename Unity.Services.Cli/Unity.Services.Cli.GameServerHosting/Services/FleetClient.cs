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

    public async Task<FleetId?> FindByName(string name, CancellationToken cancellationToken = default)
    {
        var res = await m_FleetsApiAsync.ListFleetsAsync(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId, cancellationToken: cancellationToken);
        var filtered = res.Where(f => f.Name == name).ToList();
        switch (filtered.Count(b => b.Name == name))
        {
            case 0:
                return null;
            case 1:
                return new FleetId { Id = filtered[0].Id };
            default:
                throw new DuplicateResourceException("BuildConfiguration", name);
        }
    }

    public async Task<FleetId> Create(string name, IList<BuildConfigurationId> buildConfigurations, MultiplayConfig.FleetDefinition definition, CancellationToken cancellationToken = default)
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
        return new FleetId { Id = res.Id };
    }

    public async Task Update(FleetId id, string name, IList<BuildConfigurationId> buildConfigurations, MultiplayConfig.FleetDefinition definition, CancellationToken cancellationToken = default)
    {
        var fleet = new FleetUpdateRequest(
            name: name,
            osID: Guid.Empty, // Must be set in order to avoid breaking the API
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
