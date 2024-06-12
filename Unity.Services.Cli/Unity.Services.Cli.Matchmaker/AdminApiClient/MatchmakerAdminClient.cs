using Newtonsoft.Json;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Cli.Matchmaker.Parser;
using Unity.Services.Cli.Matchmaker.Service;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using Unity.Services.Matchmaker.Authoring.Core.ConfigApi;
using Unity.Services.Matchmaker.Authoring.Core.Model;
using Generated = Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Model;

namespace Unity.Services.Cli.Matchmaker.AdminApiClient;

class MatchmakerAdminClient : IConfigApiClient
{
    readonly IMatchmakerService m_Service;
    readonly IGameServerHostingService m_GameServerHostingService;
    MultiplayResources m_RemoteMultiplayResources;

    public MatchmakerAdminClient(IMatchmakerService service, IGameServerHostingService gameServerHostingService)
    {
        m_Service = service;
        m_GameServerHostingService = gameServerHostingService;
    }

    public async Task<string> Initialize(string projectId, string environmentId, CancellationToken ct = default)
    {
        var settings = JsonConvert.DefaultSettings?.Invoke() ?? new JsonSerializerSettings();
        if (settings.Converters.All(c => c.GetType() != typeof(JsonObjectSpecializedConverter)))
            settings.Converters.Add(new JsonObjectSpecializedConverter());
        JsonConvert.DefaultSettings = () => settings;
        await m_GameServerHostingService.AuthorizeGameServerHostingService(ct);
        var fleets = m_GameServerHostingService.FleetsApi.ListFleets(Guid.Parse(projectId), Guid.Parse(environmentId));
        m_RemoteMultiplayResources = MpResourcesFromFleetAndBuildConfig(fleets);
        return await m_Service.Initialize(projectId, environmentId, ct);
    }

    static MultiplayResources MpResourcesFromFleetAndBuildConfig(List<FleetListItem> fleets)
    {
        return new MultiplayResources()
        {
            Fleets = fleets.Select(
                    f => new MultiplayResources.Fleet()
                    {
                        Name = f.Name,
                        Id = f.Id.ToString(),
                        BuildConfigs = f.BuildConfigurations
                            .Select(
                                bc => new MultiplayResources.Fleet.BuildConfig()
                                {
                                    Name = bc.Name,
                                    Id = bc.Id.ToString()
                                })
                            .ToList(),
                        QosRegions = f.Regions.Select(
                                qr => new MultiplayResources.Fleet.QosRegion()
                                {
                                    Name = qr.RegionName,
                                    Id = qr.RegionID.ToString()
                                })
                            .ToList()
                    })
                .ToList()
        };
    }

    public async Task<(bool, EnvironmentConfig)> GetEnvironmentConfig(CancellationToken ct = default)
    {

        var (exist, genEnvConfig) = await m_Service.GetEnvironmentConfig(ct);
        if (!exist)
            return (false, new EnvironmentConfig());
        return (true, new EnvironmentConfig
        {
            DefaultQueueName = new QueueName(genEnvConfig?.DefaultQueueName ?? ""),
            Enabled = genEnvConfig?.Enabled ?? false
        });
    }

    public async Task<List<ErrorResponse>> UpsertEnvironmentConfig(EnvironmentConfig environmentConfig, bool dryRun, CancellationToken ct = default)
    {
        var genEnvConfig = new Generated.EnvironmentConfig
        (
            defaultQueueName: environmentConfig.DefaultQueueName.ToString() ?? string.Empty,
            enabled: environmentConfig.Enabled
        );
        return await m_Service.UpsertEnvironmentConfig(genEnvConfig, dryRun, ct);
    }

    public async Task<List<(QueueConfig, List<ErrorResponse>)>> ListQueues(CancellationToken ct = default)
    {
        var genQueues = await m_Service.ListQueues(ct);
        return genQueues?.Select(f => ModelGeneratedToCore.FromGeneratedQueueConfig(f, m_RemoteMultiplayResources)).ToList() ?? new List<(QueueConfig, List<ErrorResponse>)>();
    }

    public async Task<List<ErrorResponse>> UpsertQueue(QueueConfig queueConfig, MultiplayResources availableMultiplayResources, bool dryRun, CancellationToken ct = default)
    {
        var (genQueueConfig, errors) = ModelCoreToGenerated.FromCoreQueueConfig(queueConfig, availableMultiplayResources, dryRun);
        if (errors.Count > 0)
            return errors;
        return await m_Service.UpsertQueueConfig(
            genQueueConfig,
            dryRun,
            ct);
    }

    public async Task DeleteQueue(QueueName queueName, bool dryRun, CancellationToken ct = default)
    {
        await m_Service.DeleteQueue(queueName.ToString() ?? string.Empty, dryRun, ct);
    }

    MultiplayResources IConfigApiClient.GetRemoteMultiplayResources() => m_RemoteMultiplayResources;

    // This class allows us to add the JsonObjectSpecializedConverter to the Core JsonObject class
    [JsonConverter(typeof(JsonObjectSpecializedConverter))]
    public class JsonObjectSpecialized : JsonObject
    {
        public JsonObjectSpecialized(string value) : base(value)
        {
        }
    }

}
