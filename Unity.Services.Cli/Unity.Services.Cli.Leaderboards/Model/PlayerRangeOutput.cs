using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;
using Newtonsoft.Json;

namespace Unity.Services.Cli.Leaderboards.Model;

class PlayerRangeOutput
{
    public List<LeaderboardScoreEntry> Results { get; set; }

    public PlayerRangeOutput(LeaderboardScores response)
    {
        Results = response.Results.Select(e => new LeaderboardScoreEntry(e)).ToList();
    }

    public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);
}
