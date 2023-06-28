using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.ModuleTemplate.Authoring.Core.Model;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Service
{
    //This is a sample IServiceClient and might not map to your existing admin APIs
    public interface IModuleTemplateClient
    {
        void Initialize(string environmentId, string projectId, CancellationToken cancellationToken);

        Task<IResource> Get(string name);
        Task Update(IResource resource);
        Task Create(IResource resource);
        Task Delete(IResource resource);
        Task<IReadOnlyList<IResource>> List();
    }
}
