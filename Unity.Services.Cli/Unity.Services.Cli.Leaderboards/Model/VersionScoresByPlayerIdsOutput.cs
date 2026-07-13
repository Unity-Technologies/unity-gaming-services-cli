using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;
using Newtonsoft.Json;

namespace Unity.Services.Cli.Leaderboards.Model;

class VersionScoresByPlayerIdsOutput
{
    public LeaderboardVersionInfo Version { get; set; }
    public List<LeaderboardScoreEntry> Results { get; set; }
    public List<string> EntriesNotFoundForPlayerIds { get; set; }

    public VersionScoresByPlayerIdsOutput(LeaderboardVersionScoresByPlayerIds response)
    {
        Version = new LeaderboardVersionInfo(response._Version);
        Results = response.Results.Select(e => new LeaderboardScoreEntry(e)).ToList();
        EntriesNotFoundForPlayerIds = response.EntriesNotFoundForPlayerIds;
    }

    public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);
}
