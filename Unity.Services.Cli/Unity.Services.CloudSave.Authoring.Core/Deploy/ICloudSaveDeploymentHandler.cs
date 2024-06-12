using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.CloudSave.Authoring.Core.Model;

namespace Unity.Services.CloudSave.Authoring.Core.Deploy
{
    public interface ICloudSaveDeploymentHandler
    {
        Task<DeployResult> DeployAsync(
            IReadOnlyList<IResourceDeploymentItem> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default);
    }

}
