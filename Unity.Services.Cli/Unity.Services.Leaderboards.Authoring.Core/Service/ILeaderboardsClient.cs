using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Leaderboards.Authoring.Core.Model;

namespace Unity.Services.Leaderboards.Authoring.Core.Service
{
    public interface ILeaderboardsClient
    {
        void Initialize(string environmentId, string projectId, CancellationToken cancellationToken);

        Task<ILeaderboardConfig> Get(string id, CancellationToken token);
        Task Update(ILeaderboardConfig leaderboardConfig, CancellationToken token);
        Task Create(ILeaderboardConfig leaderboardConfig, CancellationToken token);
        Task Delete(ILeaderboardConfig leaderboardConfig, CancellationToken token);
        Task<IReadOnlyList<ILeaderboardConfig>> List(CancellationToken token);
    }
}
