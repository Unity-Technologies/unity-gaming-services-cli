using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Triggers.Authoring.Core.Model;

namespace Unity.Services.Triggers.Authoring.Core.Service
{
    public interface ITriggersClient
    {
        void Initialize(string environmentId, string projectId, CancellationToken cancellationToken);

        Task<ITriggerConfig> Get(string name);
        Task Update(ITriggerConfig triggerConfig);
        Task Create(ITriggerConfig triggerConfig);
        Task Delete(ITriggerConfig triggerConfig);
        Task<IReadOnlyList<ITriggerConfig>> List();
    }
}
