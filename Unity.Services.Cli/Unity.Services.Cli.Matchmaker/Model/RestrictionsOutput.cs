using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Restrictions = Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Model.Restrictions;

namespace Unity.Services.Cli.Matchmaker.Model;

class RestrictionsOutput
{
    public int? MaxQueues { get; set; }
    public int? MaxPoolsPerQueue { get; set; }
    public int? MaxPoolVariants { get; set; }
    public int? MaxMatchTeams { get; set; }
    public int? MaxMatchRules { get; set; }
    public int? MaxTeamRules { get; set; }
    public int? MaxRuleRelaxations { get; set; }
    public int? MaxPlayersPerTicket { get; set; }
    public int? MaxPoolTimeout { get; set; }

    public RestrictionsOutput(Restrictions restrictions)
    {
        MaxQueues = restrictions.MaxQueues;
        MaxPoolsPerQueue = restrictions.MaxPoolsPerQueue;
        MaxPoolVariants = restrictions.MaxPoolVariants;
        MaxMatchTeams = restrictions.MaxMatchTeams;
        MaxMatchRules = restrictions.MaxMatchRules;
        MaxTeamRules = restrictions.MaxTeamRules;
        MaxRuleRelaxations = restrictions.MaxRuleRelaxations;
        MaxPlayersPerTicket = restrictions.MaxPlayersPerTicket;
        MaxPoolTimeout = restrictions.MaxPoolTimeout;
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
