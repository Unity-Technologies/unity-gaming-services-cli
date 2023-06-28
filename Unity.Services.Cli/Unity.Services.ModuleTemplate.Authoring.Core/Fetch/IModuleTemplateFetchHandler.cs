using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.ModuleTemplate.Authoring.Core.Model;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Fetch
{
    public interface IModuleTemplateFetchHandler
    {
        public Task<FetchResult> FetchAsync(
            string rootDirectory,
            IReadOnlyList<IResource> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default);
    }
}
