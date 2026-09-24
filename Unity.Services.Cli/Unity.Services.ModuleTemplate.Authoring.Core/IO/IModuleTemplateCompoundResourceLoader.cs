using System.Threading;
using System.Threading.Tasks;
using Unity.Services.ModuleTemplate.Authoring.Core.Model;

namespace Unity.Services.ModuleTemplate.Authoring.Core.IO
{
    public interface IModuleTemplateCompoundResourceLoader
    {
        Task<CompoundResourceDeploymentItem> ReadResource(string path, CancellationToken token);
        Task CreateOrUpdateResource(CompoundResourceDeploymentItem deployableItem, CancellationToken token);
        Task DeleteResource(CompoundResourceDeploymentItem deploymentItem, CancellationToken token);
    }
}
