using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Triggers.Authoring.Core.Model;

namespace Unity.Services.Triggers.Authoring.Core.Deploy
{
    public interface ITriggersDeploymentHandler
    {
        Task<DeployResult> DeployAsync(IReadOnlyList<ITriggerConfig> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default);
    }
}
