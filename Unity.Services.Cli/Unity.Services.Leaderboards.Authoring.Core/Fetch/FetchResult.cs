using System.Collections.Generic;
using Unity.Services.Leaderboards.Authoring.Core.Model;

namespace Unity.Services.Leaderboards.Authoring.Core.Fetch
{
    public class FetchResult
    {
        public List<ILeaderboardConfig> Created { get; set; }
        public List<ILeaderboardConfig> Updated { get; set; }
        public List<ILeaderboardConfig> Deleted { get; set; }
        public List<ILeaderboardConfig> Fetched { get; set; }
        public List<ILeaderboardConfig> Failed { get; set; }
    }
}
