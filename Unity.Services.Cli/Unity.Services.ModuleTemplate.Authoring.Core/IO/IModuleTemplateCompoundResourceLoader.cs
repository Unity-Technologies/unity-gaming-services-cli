using System.Threading;
using System.Threading.Tasks;
using Unity.Services.ModuleTemplate.Authoring.Core.Model;

namespace Unity.Services.ModuleTemplate.Authoring.Core.IO
{
    public interface IModuleTemplateCompoundResourceLoader
    {
        Task<ICompoundResourceDeploymentItem> ReadResource(string path, CancellationToken token);
        Task CreateOrUpdateResource(ICompoundResourceDeploymentItem deployableItem, CancellationToken token);
        Task DeleteResource(ICompoundResourceDeploymentItem deploymentItem, CancellationToken token);
    }
}
