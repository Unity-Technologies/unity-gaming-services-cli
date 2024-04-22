using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.CloudContentDelivery.Authoring.Core.Model;

namespace Unity.Services.CloudContentDelivery.Authoring.Core.Service
{
    //This is a sample IServiceClient and might not map to your existing admin APIs
    public interface ICloudContentDeliveryClient
    {
        void Initialize(string environmentId, string projectId, CancellationToken cancellationToken);

        Task<IBucket> Get(string name);
        Task Update(IBucket bucket);
        Task Create(IBucket bucket);
        Task Delete(IBucket bucket);
        Task<IReadOnlyList<IBucket>> List();
    }
}
