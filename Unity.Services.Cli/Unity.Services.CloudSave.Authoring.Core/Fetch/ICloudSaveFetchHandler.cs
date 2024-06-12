using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.CloudSave.Authoring.Core.Model;

namespace Unity.Services.CloudSave.Authoring.Core.Fetch
{
    public interface ICloudSaveFetchHandler
    {
        public Task<FetchResult> FetchAsync(
            string rootDirectory,
            IReadOnlyList<IResourceDeploymentItem> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default);
    }
}
