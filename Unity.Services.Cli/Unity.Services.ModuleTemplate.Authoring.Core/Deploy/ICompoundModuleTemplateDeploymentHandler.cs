using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.ModuleTemplate.Authoring.Core.Model;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Deploy
{
    public interface ICompoundModuleTemplateDeploymentHandler
    {
        Task<DeployResult> DeployAsync(
            IReadOnlyList<ICompoundResourceDeploymentItem> compoundLocalResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default);
    }
}
