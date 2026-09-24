using System.Threading;
using System.Threading.Tasks;
using Unity.Services.ModuleTemplate.Authoring.Core.Model;

namespace Unity.Services.ModuleTemplate.Authoring.Core.IO
{
    public interface IModuleTemplateSimpleResourceLoader
    {
        Task<SimpleResourceDeploymentItem> ReadResource(string path, CancellationToken token);
        Task CreateOrUpdateResource(SimpleResourceDeploymentItem deployableItem, CancellationToken token);
        Task DeleteResource(SimpleResourceDeploymentItem deploymentItem, CancellationToken token);
    }
}
