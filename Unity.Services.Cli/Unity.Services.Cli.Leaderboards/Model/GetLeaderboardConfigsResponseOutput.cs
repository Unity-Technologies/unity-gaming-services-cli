using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.Leaderboards.Model;

class GetLeaderboardConfigsResponseOutput
{
    public List<LeaderboardConfigInner> Leaderboards;
    public GetLeaderboardConfigsResponseOutput(IEnumerable<UpdatedLeaderboardConfig> leaderboards)
    {
        Leaderboards = leaderboards.Select(l => new LeaderboardConfigInner(l)).ToList();
    }
    public override string ToString()
    {
        var jsonString = JsonConvert.SerializeObject(Leaderboards);
        var formattedJson = JToken.Parse(jsonString).ToString(Formatting.Indented);
        return formattedJson;
    }

}

class LeaderboardConfigInner
{
    public string Id { get; set; }
    public string Name { get; set; }

    public LeaderboardConfigInner(UpdatedLeaderboardConfig response)
    {
        Id = response.Id;
        Name = response.Name;
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



