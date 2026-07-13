using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;
using Newtonsoft.Json;

namespace Unity.Services.Cli.Leaderboards.Model;

class LeaderboardTierScoresOutput
{
    public string Tier { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
    public List<LeaderboardScoreEntry> Results { get; set; }

    public LeaderboardTierScoresOutput(LeaderboardTierScoresPage response)
    {
        Tier = response.Tier;
        Offset = response.Offset;
        Limit = response.Limit;
        Total = response.Total;
        Results = response.Results.Select(e => new LeaderboardScoreEntry(e)).ToList();
    }

    public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);
}
