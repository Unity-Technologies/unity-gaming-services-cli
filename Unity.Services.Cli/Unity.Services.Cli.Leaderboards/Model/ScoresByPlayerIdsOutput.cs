using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;
using Newtonsoft.Json;

namespace Unity.Services.Cli.Leaderboards.Model;

class ScoresByPlayerIdsOutput
{
    public List<LeaderboardScoreEntry> Results { get; set; }
    public List<string> EntriesNotFoundForPlayerIds { get; set; }

    public ScoresByPlayerIdsOutput(LeaderboardScoresWithNotFoundPlayerIds response)
    {
        Results = response.Results.Select(e => new LeaderboardScoreEntry(e)).ToList();
        EntriesNotFoundForPlayerIds = response.EntriesNotFoundForPlayerIds;
    }

    public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);
}
