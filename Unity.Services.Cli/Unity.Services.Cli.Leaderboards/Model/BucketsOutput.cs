using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;
using Newtonsoft.Json;

namespace Unity.Services.Cli.Leaderboards.Model;

class BucketsOutput
{
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
    public List<Guid> Results { get; set; }

    public BucketsOutput(BucketsPage response)
    {
        Offset = response.Offset;
        Limit = response.Limit;
        Total = response.Total;
        Results = response.Results;
    }

    public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);
}
