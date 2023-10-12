using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Access.Authoring.Core.Model;

namespace Unity.Services.Access.Authoring.Core.Service
{
    public interface IProjectAccessClient
    {
        void Initialize(string environmentId, string projectId, CancellationToken cancellationToken);

        Task<List<AccessControlStatement>> GetAsync();
        Task UpsertAsync(IReadOnlyList<AccessControlStatement> authoringStatements);
        Task DeleteAsync(IReadOnlyList<AccessControlStatement> authoringStatements);
    }
}
