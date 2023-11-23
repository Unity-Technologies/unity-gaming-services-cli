using Unity.Services.Leaderboards.Authoring.Core.Model;
using Unity.Services.Leaderboards.Authoring.Core.Serialization;

namespace Unity.Services.Cli.Leaderboards.Deploy;

class LeaderboardsSerializer : ILeaderboardsSerializer
{
    public string Serialize(ILeaderboardConfig config)
    {
        var fileName = Path.GetFileNameWithoutExtension(config.Path);
        var leaderboardFile = new LeaderboardConfigFile(
            fileName == config.Id ? null : config.Id,
            config.Name,
            config.SortOrder,
            config.UpdateType)
        {
            BucketSize = config.BucketSize,
            ResetConfig = config.ResetConfig,
            TieringConfig = config.TieringConfig,
        };
        return leaderboardFile.FileBodyText;
    }
}
