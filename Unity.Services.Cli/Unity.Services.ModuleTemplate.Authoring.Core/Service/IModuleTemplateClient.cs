using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.ModuleTemplate.Authoring.Core.Model;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Service
{
    //This is a sample IServiceClient and might not map to your existing admin APIs
    public interface IModuleTemplateClient
    {
        Task Initialize(string environmentId, string projectId, CancellationToken cancellationToken);

        Task<IResource> Get(string id, CancellationToken cancellationToken);
        Task Update(IResource resource, CancellationToken cancellationToken);
        Task Create(IResource resource, CancellationToken cancellationToken);
        Task Delete(IResource resource, CancellationToken cancellationToken);
        Task<IReadOnlyList<IResource>> List(CancellationToken cancellationToken);
        Task<string> RawGetRequest(string address, CancellationToken cancellationToken = default);
    }
}
