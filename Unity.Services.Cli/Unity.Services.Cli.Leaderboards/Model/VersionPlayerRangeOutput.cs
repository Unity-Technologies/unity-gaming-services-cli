using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;
using Newtonsoft.Json;

namespace Unity.Services.Cli.Leaderboards.Model;

class VersionPlayerRangeOutput
{
    public LeaderboardVersionInfo Version { get; set; }
    public List<LeaderboardScoreEntry> Results { get; set; }

    public VersionPlayerRangeOutput(LeaderboardVersionRange response)
    {
        Version = new LeaderboardVersionInfo(response._Version);
        Results = response.Results.Select(e => new LeaderboardScoreEntry(e)).ToList();
    }

    public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);
}
