using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.Model;

class LeaderboardVersionInfo
{
    public string Id { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    public LeaderboardVersionInfo(LeaderboardVersion version)
    {
        Id = version.Id;
        Start = version.Start;
        End = version.End;
    }
}
