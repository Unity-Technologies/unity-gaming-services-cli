using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.SchedulerApiV1.Generated.Api;
using Unity.Services.Gateway.SchedulerApiV1.Generated.Client;
using Unity.Services.Gateway.SchedulerApiV1.Generated.Model;
using Unity.Services.Scheduler.Authoring.Core.Model;
using Unity.Services.Scheduler.Authoring.Core.Service;
using ScheduleConfig = Unity.Services.Scheduler.Authoring.Core.Model.ScheduleConfig;

namespace Unity.Services.Cli.Scheduler.Deploy;

class SchedulerClient : ISchedulerClient
{
    readonly ISchedulerApiAsync m_SchedulerApi;
    readonly IServiceAccountAuthenticationService  m_AuthenticationService;
    readonly IConfigurationValidator m_Validator;
    internal Guid ProjectId { get; set; }
    internal Guid EnvironmentId { get; set; }
    internal CancellationToken CancellationToken { get; set; }

    public SchedulerClient(ISchedulerApiAsync schedulerApi,
        IServiceAccountAuthenticationService  authenticationService,
        IConfigurationValidator validator)
    {
        m_SchedulerApi = schedulerApi;
        m_AuthenticationService = authenticationService;
        m_Validator = validator;
    }

    public async Task Initialize(
        string environmentId,
        string projectId,
        CancellationToken cancellationToken)
    {
        ProjectId = new Guid(projectId);
        EnvironmentId = new Guid(environmentId);
        CancellationToken = cancellationToken;

        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
    }

    public async Task AuthorizeServiceAsync(CancellationToken cancellationToken = default)
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        m_SchedulerApi.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }

    public void ValidateProjectIdAndEnvironmentId(string projectId, string environmentId)
    {
        m_Validator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_Validator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);
    }

    public async Task<IScheduleConfig> Get(string id)
    {
        var schedule = await m_SchedulerApi.GetScheduleConfigAsync(
            ProjectId,
            EnvironmentId,
            new Guid(id),
            0,
            CancellationToken);
        return FromResponse(schedule);
    }

    public async Task Update(IScheduleConfig resource)
    {
        await m_SchedulerApi.DeleteScheduleConfigAsync(
            ProjectId,
            EnvironmentId,
            new Guid(resource.Id),
            0,
            CancellationToken);
        await m_SchedulerApi.CreateScheduleConfigAsync(
            ProjectId,
            EnvironmentId,
            new ScheduleConfigBody(
                resource.Name,
                resource.EventName,
                resource.ScheduleType,
                resource.Schedule,
                resource.PayloadVersion,
                resource.Payload),
            0,
            CancellationToken);
    }

    public async Task Create(IScheduleConfig resource)
    {
        await m_SchedulerApi.CreateScheduleConfigAsync(
            ProjectId,
            EnvironmentId,
            new ScheduleConfigBody(
                resource.Name,
                resource.EventName,
                resource.ScheduleType,
                resource.Schedule,
                resource.PayloadVersion,
                resource.Payload),
            0,
            CancellationToken);
    }

    public async Task Delete(IScheduleConfig resource)
    {
        await m_SchedulerApi.DeleteScheduleConfigAsync(
            ProjectId,
            EnvironmentId,
            new Guid(resource.Id),
            0,
            CancellationToken);
    }

    public async Task<IReadOnlyList<IScheduleConfig>> List()
    {
        const int limit = 50;
        var schedules = new List<Gateway.SchedulerApiV1.Generated.Model.ScheduleConfig>();
        string? cursor = null;
        List<Gateway.SchedulerApiV1.Generated.Model.ScheduleConfig> newBatch;
        do
        {
            var results = await m_SchedulerApi.ListSchedulerConfigsAsync(
                ProjectId,
                EnvironmentId,
                limit,
                cursor,
                0,
                CancellationToken);
            newBatch = results.Configs;
            cursor = newBatch.LastOrDefault()?.Id.ToString();
            schedules.AddRange(newBatch);

            if (CancellationToken.IsCancellationRequested)
                break;
        } while (newBatch.Count >= limit);

        return schedules.Select(FromResponse).ToList();
    }

    static ScheduleConfig FromResponse(Gateway.SchedulerApiV1.Generated.Model.ScheduleConfig response)
    {
        return new ScheduleConfig(
            response.Name,
            response.EventName,
            response.Type,
            response.Schedule,
            response.PayloadVersion,
            response.Payload)
        {
            Id = response.Id.ToString(),
            Path = "Remote"
        };
    }
}
