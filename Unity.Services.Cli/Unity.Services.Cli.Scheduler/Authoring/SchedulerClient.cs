using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.SchedulerApiV1.Generated.Api;
using Unity.Services.Gateway.SchedulerApiV1.Generated.Model;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Service;
using SchedulerApiException = Unity.Services.Gateway.SchedulerApiV1.Generated.Client.ApiException;

namespace Unity.Services.Cli.Scheduler.Deploy;

class SchedulerClient : ISchedulerClient
{
    readonly ISchedulerApiAsync m_SchedulerApi;
    readonly IServiceAccountAuthenticationService m_AuthenticationService;
    readonly IConfigurationValidator m_Validator;
    internal Guid ProjectId { get; private set; }
    internal Guid EnvironmentId { get; private set; }

    public SchedulerClient(
        ISchedulerApiAsync schedulerApi,
        IServiceAccountAuthenticationService authenticationService,
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

        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
    }

    async Task AuthorizeServiceAsync(CancellationToken cancellationToken = default)
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        m_SchedulerApi.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }

    void ValidateProjectIdAndEnvironmentId(string projectId, string environmentId)
    {
        m_Validator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_Validator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);
    }

    public async Task<SchedulerEntry> Get(string id,  CancellationToken cancellationToken = default)
    {
        return await ExecuteRequest(async () =>
        {
            var schedule = await m_SchedulerApi.GetScheduleConfigAsync(
                ProjectId,
                EnvironmentId,
                new Guid(id),
                cancellationToken: cancellationToken);
            return FromResponse(schedule);
        });
    }

    public async Task Update(SchedulerEntry resource, CancellationToken cancellationToken = default)
    {
        await ExecuteRequest(async () =>
        {
            await m_SchedulerApi.DeleteScheduleConfigAsync(
                ProjectId,
                EnvironmentId,
                new Guid(resource.Id),
                cancellationToken: cancellationToken);
            await m_SchedulerApi.CreateScheduleConfigAsync(
                ProjectId,
                EnvironmentId,
                ToRequest(resource),
                cancellationToken: cancellationToken);
        });
    }

    public async Task Create(SchedulerEntry resource, CancellationToken cancellationToken = default)
    {
        await ExecuteRequest(() => m_SchedulerApi.CreateScheduleConfigAsync(
                ProjectId,
                EnvironmentId,
                ToRequest(resource),
                cancellationToken: cancellationToken));
    }

    public async Task Delete(SchedulerEntry resource,  CancellationToken cancellationToken = default)
    {
        await ExecuteRequest(() => m_SchedulerApi.DeleteScheduleConfigAsync(
                ProjectId,
                EnvironmentId,
                new Guid(resource.Id),
                cancellationToken: cancellationToken));
    }

    static async Task ExecuteRequest(Func<Task> request)
    {
        try
        {
            await request();
        }
        catch (SchedulerApiException e)
        {
            throw new ClientException(e.Message, e);
        }
    }

    static async Task<T> ExecuteRequest<T>(Func<Task<T>> request)
    {
        try
        {
            return await request();
        }
        catch (SchedulerApiException e)
        {
            throw new ClientException(e.Message, e);
        }
    }

    public async Task<IReadOnlyList<SchedulerEntry>> List(CancellationToken cancellationToken = default)
    {
        return await ExecuteRequest(async () =>
        {
            const int limit = 50;
            var schedules = new List<ScheduleConfig>();
            string? cursor = null;
            List<ScheduleConfig> newBatch;
            do
            {
                var results = await m_SchedulerApi.ListSchedulerConfigsAsync(
                    ProjectId,
                    EnvironmentId,
                    limit,
                    cursor,
                    cancellationToken: cancellationToken);
                newBatch = results.Configs;
                cursor = newBatch.LastOrDefault()?.Id.ToString();
                schedules.AddRange(newBatch);

                if (cancellationToken.IsCancellationRequested)
                    break;
            } while (newBatch.Count >= limit);

            return schedules.Select(FromResponse).ToList();
        });
    }

    static ScheduleConfigBody ToRequest(SchedulerEntry resource)
    {
        var request = new ScheduleConfigBody(
            name: resource.Name,
            eventName: resource.EventName,
            type: resource.ScheduleType,
            schedule: resource.Schedule,
            payloadVersion: resource.PayloadVersion,
            payload: resource.Payload);

        if (resource.StartAt.HasValue)
        {
            request.StartAt = resource.StartAt.Value;
        }

        if (resource.EndAt.HasValue)
        {
            request.EndAt = resource.EndAt.Value;
        }

        return request;
    }

    internal static SchedulerEntry FromResponse(ScheduleConfig response)
    {
        return new SchedulerEntry
        {
            Id = response.Id.ToString(),
            Name = response.Name,
            EventName = response.EventName,
            StartAt = response.StartAt,
            EndAt = response.EndAt,
            PayloadVersion = response.PayloadVersion,
            Payload = response.Payload,
            ScheduleType = response.Type,
            Schedule = response.Schedule
        };
    }
}
