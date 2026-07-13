using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.Model;

class LeaderboardScoreEntry
{
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
    public double Score { get; set; }
    public int Rank { get; set; }
    public string? Tier { get; set; }

    public LeaderboardScoreEntry(LeaderboardEntry entry)
    {
        PlayerId = entry.PlayerId;
        PlayerName = entry.PlayerName;
        Score = entry.Score;
        Rank = entry.Rank;
        Tier = entry.Tier;
    }
}
