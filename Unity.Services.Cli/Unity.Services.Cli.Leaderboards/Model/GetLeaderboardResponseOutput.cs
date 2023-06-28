using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.Leaderboards.Model;

class GetLeaderboardResponseOutput
{
    public SortOrder SortOrder { get; set; }
    public UpdateType UpdateType { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public decimal BucketSize { get; set; }
    public ResetConfig? ResetConfig { get; set; }
    public TieringConfig? TieringConfig { get; set; }
    public DateTime? Updated { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? LastReset { get; set; }
    public List<string> Versions { get; set; }
    public GetLeaderboardResponseOutput(UpdatedLeaderboardConfig response)
    {
        Id = response.Id;
        Name = response.Name;
        SortOrder = response.SortOrder;
        UpdateType = response.UpdateType;
        BucketSize = response.BucketSize;
        ResetConfig = response.ResetConfig;
        TieringConfig = response.TieringConfig;
        Updated = response.Updated;
        Created = response.Created;
        LastReset = response.LastReset;
        Versions = response.Versions.Select(v => v.Id).ToList();
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


