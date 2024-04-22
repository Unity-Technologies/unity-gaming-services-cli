using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.CloudContentDelivery.Authoring.Core.Model;

namespace Unity.Services.CloudContentDelivery.Authoring.Core.Deploy
{
    public interface ICloudContentDeliveryDeploymentHandler
    {
        Task<DeployResult> DeployAsync(IReadOnlyList<IBucket> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default);
    }
}
