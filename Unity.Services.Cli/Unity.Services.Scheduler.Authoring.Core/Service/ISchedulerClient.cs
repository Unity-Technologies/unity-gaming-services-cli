using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Scheduler.Authoring.Core.Model;

namespace Unity.Services.Scheduler.Authoring.Core.Service
{
    //This is a sample IServiceClient and might not map to your existing admin APIs
    public interface ISchedulerClient
    {
        Task Initialize(string environmentId, string projectId, CancellationToken cancellationToken);

        Task<IScheduleConfig> Get(string id);
        Task Update(IScheduleConfig resource);
        Task Create(IScheduleConfig resource);
        Task Delete(IScheduleConfig resource);
        Task<IReadOnlyList<IScheduleConfig>> List();
    }
}
