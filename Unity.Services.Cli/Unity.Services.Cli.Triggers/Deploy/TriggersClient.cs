using Newtonsoft.Json;
using Unity.Services.Cli.Triggers.Service;
using Unity.Services.Gateway.TriggersApiV1.Generated.Model;
using Unity.Services.Triggers.Authoring.Core.Model;
using Unity.Services.Triggers.Authoring.Core.Service;
using TriggerConfig = Unity.Services.Triggers.Authoring.Core.Model.TriggerConfig;

namespace Unity.Services.Cli.Triggers.Deploy;

class TriggersClient : ITriggersClient
{
    readonly ITriggersService m_Service;

    public string ProjectId { get; set; }
    public string EnvironmentId { get; set; }
    public CancellationToken CancellationToken { get; set; }

    public TriggersClient(
        ITriggersService service,
        string projectId = "",
        string environmentId = "",
        CancellationToken cancellationToken = default)
    {
        m_Service = service;
        ProjectId = projectId;
        EnvironmentId = environmentId;
        CancellationToken = cancellationToken;
    }

    public void Initialize(
        string environmentId,
        string projectId,
        CancellationToken cancellationToken)
    {
        EnvironmentId = environmentId;
        ProjectId = projectId;
        CancellationToken = cancellationToken;
    }

    public async Task<ITriggerConfig> Get(string name)
    {
        var triggers = await m_Service.GetTriggersAsync(
            ProjectId,
            EnvironmentId,
            null,
            CancellationToken);
        var trigger = triggers.Single(t => t.Name == name);
        return FromResponse(trigger);
    }

    public async Task Update(ITriggerConfig triggerConfig)
    {
        await m_Service.UpdateTriggerAsync(
            ProjectId,
            EnvironmentId,
            triggerConfig.Id,
            new TriggerConfigBody(
                triggerConfig.Name,
                triggerConfig.EventType,
                JsonConvert.DeserializeObject<TriggerActionType>($"\"{triggerConfig.ActionType}\""),
                triggerConfig.ActionUrn),
            CancellationToken);
    }

    public async Task Create(ITriggerConfig triggerConfig)
    {
        await m_Service.CreateTriggerAsync(
            ProjectId,
            EnvironmentId,
            new TriggerConfigBody(
                triggerConfig.Name,
                triggerConfig.EventType,
                JsonConvert.DeserializeObject<TriggerActionType>($"\"{triggerConfig.ActionType}\""),
                triggerConfig.ActionUrn),
            CancellationToken);
    }

    public async Task Delete(ITriggerConfig triggerConfig)
    {
        await m_Service.DeleteTriggerAsync(
            ProjectId,
            EnvironmentId,
            triggerConfig.Id,
            CancellationToken);
    }

    public async Task<IReadOnlyList<ITriggerConfig>> List()
    {
        var triggers = await m_Service.GetTriggersAsync(
            ProjectId,
            EnvironmentId,
            null,
            CancellationToken);
        return triggers.Select(FromResponse).ToList();
    }

    static TriggerConfig FromResponse(Gateway.TriggersApiV1.Generated.Model.TriggerConfig responseConfig)
    {
        return new TriggerConfig()
        {
            ActionType = JsonConvert.SerializeObject(responseConfig.ActionType).Replace("\"", ""),
            ActionUrn = responseConfig.ActionUrn,
            EventType = responseConfig.EventType,
            Id = responseConfig.Id.ToString(),
            Name = responseConfig.Name,
            Path = "Remote",
        };
    }
}
