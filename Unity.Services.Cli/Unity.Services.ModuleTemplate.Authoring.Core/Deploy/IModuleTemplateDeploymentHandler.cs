using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.ModuleTemplate.Authoring.Core.Model;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Deploy
{
    public interface IModuleTemplateDeploymentHandler
    {
        Task<DeployResult> DeployAsync(IReadOnlyList<IResource> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default);
    }
}
