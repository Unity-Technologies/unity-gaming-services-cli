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

        Task<SimpleResource> Get(string id, CancellationToken cancellationToken);
        Task Update(SimpleResource resource, CancellationToken cancellationToken);
        Task Create(SimpleResource resource, CancellationToken cancellationToken);
        Task Delete(SimpleResource resource, CancellationToken cancellationToken);
        Task<IReadOnlyList<SimpleResource>> List(CancellationToken cancellationToken);
        Task<string> RawGetRequest(string address, CancellationToken cancellationToken = default);
    }
}
