using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.ModuleTemplate.Authoring.Core.Model;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Fetch
{
    public interface ICompoundModuleTemplateFetchHandler
    {
        public Task<FetchResult> FetchAsync(
            string rootDirectory,
            IReadOnlyList<ICompoundResourceDeploymentItem> compoundLocalResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default);
    }
}
