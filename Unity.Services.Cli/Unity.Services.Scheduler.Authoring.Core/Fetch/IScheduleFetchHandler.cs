using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Scheduler.Authoring.Core.Model;

namespace Unity.Services.Scheduler.Authoring.Core.Fetch
{
    public interface IScheduleFetchHandler
    {
        public Task<FetchResult> FetchAsync(
            string rootDirectory,
            IReadOnlyList<IScheduleConfig> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default);
    }
}
