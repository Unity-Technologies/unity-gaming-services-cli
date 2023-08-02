using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Leaderboards.Authoring.Core.Model;

namespace Unity.Services.Leaderboards.Authoring.Core.Fetch
{
    public interface ILeaderboardsFetchHandler
    {
        public Task<FetchResult> FetchAsync(
            string rootDirectory,
            IReadOnlyList<ILeaderboardConfig> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default);
    }
}
