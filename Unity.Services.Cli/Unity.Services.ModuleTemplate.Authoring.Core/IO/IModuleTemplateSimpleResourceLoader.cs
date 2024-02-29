using System.Threading;
using System.Threading.Tasks;
using Unity.Services.ModuleTemplate.Authoring.Core.Model;

namespace Unity.Services.ModuleTemplate.Authoring.Core.IO
{
    public interface IModuleTemplateSimpleResourceLoader
    {
        Task<IResourceDeploymentItem> ReadResource(string path, CancellationToken token);
        Task CreateOrUpdateResource(IResourceDeploymentItem deployableItem, CancellationToken token);
        Task DeleteResource(IResourceDeploymentItem deploymentItem, CancellationToken token);
    }
}
