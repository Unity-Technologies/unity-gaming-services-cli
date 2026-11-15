using Unity.Services.Gateway.SchedulerApiV1.Generated.Model;

namespace Unity.Services.Cli.Scheduler.Service;

interface ISchedulerStatusClient
{
    Task Initialize(string environmentId, string projectId, CancellationToken cancellationToken);


    Task UpdateStatus(string id, ScheduleStatusUpdate status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduleConfig>> ListConfigs(CancellationToken cancellationToken = default);
}
