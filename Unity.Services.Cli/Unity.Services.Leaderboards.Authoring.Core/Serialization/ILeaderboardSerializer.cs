using Unity.Services.Leaderboards.Authoring.Core.Model;

namespace Unity.Services.Leaderboards.Authoring.Core.Serialization
{
    public interface ILeaderboardsSerializer
    {
        string Serialize(ILeaderboardConfig config);
    }
}
