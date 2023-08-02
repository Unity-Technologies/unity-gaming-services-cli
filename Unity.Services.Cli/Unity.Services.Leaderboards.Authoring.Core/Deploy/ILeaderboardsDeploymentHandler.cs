using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Leaderboards.Authoring.Core.Model;

namespace Unity.Services.Leaderboards.Authoring.Core.Deploy
{
    public interface ILeaderboardsDeploymentHandler
    {
        Task<DeployResult> DeployAsync(IReadOnlyList<ILeaderboardConfig> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default);
    }
}
