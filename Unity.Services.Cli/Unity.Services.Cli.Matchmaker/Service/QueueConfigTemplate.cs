using Newtonsoft.Json;
using Unity.Services.Cli.Authoring.Templates;
using Unity.Services.Cli.Matchmaker.Parser;
using Unity.Services.Matchmaker.Authoring.Core.Model;
using Unity.Services.Matchmaker.Authoring.Core.Parser;
using Range = Unity.Services.Matchmaker.Authoring.Core.Model.Range;

namespace Unity.Services.Cli.Matchmaker.Service;

class QueueConfigTemplate : QueueConfig, IFileTemplate
{
    public QueueConfigTemplate()
    {
        Name = new QueueName("default-queue");
        Enabled = true;
        MaxPlayersPerTicket = 2;
        DefaultPool = new BasePoolConfig
        {
            Name = new PoolName("default-pool"),
            Enabled = true,
            TimeoutSeconds = 90,
            MatchLogic = new MatchLogicRulesConfig
            {
                Name = "Default Pool Rules",
                MatchDefinition = new RuleBasedMatchDefinition()
                {
                    matchRules = new List<Rule>()
                    {
                        new Rule()
                        {
                            name = "skill-diff",
                            type = RuleType.Difference,
                            reference = new JsonObject("500"),
                            source = "Players.ExternalData.CloudSave.Skill"
                        },
                        new Rule()
                        {
                            name = "QoS",
                            type = RuleType.LessThanEqual,
                            reference = new JsonObject("100"),
                            source = "Players.QosResults.Latency",
                            relaxations = new List<RuleRelaxation>()
                            {
                                new RuleRelaxation()
                                {
                                    ageType = AgeType.Oldest,
                                    atSeconds = 30,
                                    type = RuleRelaxationType.ReferenceControlReplace,
                                    value = new JsonObject("200")
                                }
                            }
                        }
                    },
                    teams = new List<RuleBasedTeamDefinition>()
                    {
                        new RuleBasedTeamDefinition()
                        {
                            name = "Team",
                            playerCount = new Range()
                            {
                                min = 1,
                                max = 2
                            },
                            teamCount = new Range()
                            {
                                min = 2,
                                max = 2
                            }
                        }
                    }
                }
            },
            MatchHosting = new MultiplayConfig
            {
                Type = IMatchHostingConfig.MatchHostingType.Multiplay,
                FleetName = "my fleet",
                BuildConfigurationName = "my build configuration",
                DefaultQoSRegionName = "North America"
            }
        };
    }

    [JsonIgnore] public string Extension => IMatchmakerConfigParser.QueueConfigExtension;

    [JsonIgnore]
    public string FileBodyText => JsonConvert.SerializeObject(new QueueConfigTemplate(), MatchmakerConfigParser.JsonSerializerSettings);
}
