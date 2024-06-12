using System.Threading;
using System.Threading.Tasks;
using Unity.Services.CloudSave.Authoring.Core.Model;

namespace Unity.Services.CloudSave.Authoring.Core.IO
{
    public interface ICloudSaveSimpleResourceLoader
    {
        Task<IResourceDeploymentItem> ReadResource(string path, CancellationToken token);
        Task CreateOrUpdateResource(IResourceDeploymentItem deployableItem, CancellationToken token);
        Task DeleteResource(IResourceDeploymentItem deploymentItem, CancellationToken token);
    }
}
