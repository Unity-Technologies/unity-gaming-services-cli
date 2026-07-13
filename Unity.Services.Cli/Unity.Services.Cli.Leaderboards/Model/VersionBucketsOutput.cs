using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;
using Newtonsoft.Json;

namespace Unity.Services.Cli.Leaderboards.Model;

class VersionBucketsOutput
{
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
    public LeaderboardVersionInfo Version { get; set; }
    public List<Guid> Results { get; set; }

    public VersionBucketsOutput(VersionBucketsPage response)
    {
        Offset = response.Offset;
        Limit = response.Limit;
        Total = response.Total;
        Version = new LeaderboardVersionInfo(response._Version);
        Results = response.Results;
    }

    public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);
}
