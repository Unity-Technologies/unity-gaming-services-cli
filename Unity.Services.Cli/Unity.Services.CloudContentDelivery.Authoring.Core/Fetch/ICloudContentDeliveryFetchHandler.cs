using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.CloudContentDelivery.Authoring.Core.Model;

namespace Unity.Services.CloudContentDelivery.Authoring.Core.Fetch
{
    public interface ICloudContentDeliveryFetchHandler
    {
        public Task<FetchResult> FetchAsync(
            string rootDirectory,
            IReadOnlyList<IBucket> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default);
    }
}
