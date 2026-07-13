using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.Leaderboards.Model;

class PlayerScoreOutput
{
    public DateTime UpdatedTime { get; set; }
    public Guid BucketId { get; set; }
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
    public double Score { get; set; }
    public int Rank { get; set; }
    public string? Tier { get; set; }

    public PlayerScoreOutput(LeaderboardEntryWithUpdatedTime response)
    {
        UpdatedTime = response.UpdatedTime;
        BucketId = response.BucketId;
        PlayerId = response.PlayerId;
        PlayerName = response.PlayerName;
        Score = response.Score;
        Rank = response.Rank;
        Tier = response.Tier;
    }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
