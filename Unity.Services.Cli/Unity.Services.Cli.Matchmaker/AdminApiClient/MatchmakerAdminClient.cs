using Newtonsoft.Json;
using Unity.Services.Cli.Matchmaker.Parser;
using Unity.Services.Cli.Matchmaker.Service;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.ConfigApi;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Model;
using Generated = Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Model;

namespace Unity.Services.Cli.Matchmaker.AdminApiClient;

class MatchmakerAdminClient : IConfigApiClient
{
    readonly IMatchmakerService m_Service;

    public MatchmakerAdminClient(IMatchmakerService service)
    {
        m_Service = service;
    }

    public async Task Initialize(string projectId, string environmentId, CancellationToken ct = default)
    {
        var settings = JsonConvert.DefaultSettings?.Invoke() ?? new JsonSerializerSettings();
        if (settings.Converters.All(c => c.GetType() != typeof(JsonObjectSpecializedConverter)))
            settings.Converters.Add(new JsonObjectSpecializedConverter());
        JsonConvert.DefaultSettings = () => settings;
        _ = await m_Service.Initialize(projectId, environmentId, ct);
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
        var emptyResources = new MultiplayResources();
        return genQueues?.Select(f => ModelGeneratedToCore.FromGeneratedQueueConfig(f, emptyResources)).ToList() ?? new List<(QueueConfig, List<ErrorResponse>)>();
    }

    public async Task<List<ErrorResponse>> UpsertQueue(QueueConfig queueConfig, MultiplayResources availableMultiplayResources, bool dryRun, CancellationToken ct = default)
    {
        var (genQueueConfig, errors) = ModelCoreToGenerated.FromCoreQueueConfig(queueConfig, new MultiplayResources(), dryRun);
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

    MultiplayResources IConfigApiClient.GetRemoteMultiplayResources() => new MultiplayResources();

    [JsonConverter(typeof(JsonObjectSpecializedConverter))]
    public class JsonObjectSpecialized : JsonObject
    {
        public JsonObjectSpecialized(string value) : base(value)
        {
        }
    }

    Task IConfigApiClient.UpdateToken() => Task.CompletedTask;
}
