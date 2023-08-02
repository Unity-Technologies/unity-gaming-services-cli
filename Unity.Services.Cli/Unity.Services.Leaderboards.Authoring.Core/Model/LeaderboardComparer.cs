using System.Collections.Generic;

namespace Unity.Services.Leaderboards.Authoring.Core.Model
{
    public class LeaderboardComparer : IEqualityComparer<ILeaderboardConfig>
    {
        public bool Equals(ILeaderboardConfig x, ILeaderboardConfig y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Id == y.Id;
        }

        public int GetHashCode(ILeaderboardConfig obj)
        {
            return (obj.Id != null ? obj.Id.GetHashCode() : 0);
        }
    }
}
