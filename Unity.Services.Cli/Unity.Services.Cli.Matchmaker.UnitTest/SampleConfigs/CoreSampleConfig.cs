using Core = Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Model;

namespace Unity.Services.Cli.Matchmaker.UnitTest.SampleConfigs;

class CoreSampleConfig
{
    internal Core.QueueConfig QueueConfig = new Core.QueueConfig()
    {
        Enabled = true,
        MaxPlayersPerTicket = 1,
        Name = new Core.QueueName("DefaultQueueTest"),
        DefaultPool = new Core.BasePoolConfig()
        {
            Enabled = true,
            Name = new Core.PoolName("TestPool"),
            TimeoutSeconds = 0,
            MatchHosting = new Core.MatchIdConfig(),
            MatchLogic = new Core.MatchLogicRulesConfig()
            {
                Name = "TestMatchLogic",
                BackfillEnabled = false,
                MatchDefinition = new Core.RuleBasedMatchDefinition
                {
                    teams = new List<Core.RuleBasedTeamDefinition>(),
                    matchRules = new List<Core.Rule>()
                    {
                        new Core.Rule()
                        {
                            type = Core.RuleType.Difference,
                            source = "Player.Skill",
                            name = "Skill",
                            reference = new AdminApiClient.MatchmakerAdminClient.JsonObjectSpecialized(
                                JsonSampleConfigLoader.WindowsLineEnding("[\n  \"test_string\"\n]")),
                            enableRule = true,
                            not = true,
                            relaxations = new List<Core.RuleRelaxation>()
                            {
                                new Core.RuleRelaxation()
                                {
                                    type = Core.RuleRelaxationType.ReferenceControlReplace,
                                    ageType = Core.AgeType.Oldest,
                                    atSeconds = 30.0,
                                    value = new AdminApiClient.MatchmakerAdminClient.JsonObjectSpecialized("100"),
                                }
                            }
                        },
                        new Core.Rule()
                        {
                            name = "CloudSaveElo",
                            source = "ExternalData.CloudSave.myObject",
                            type = Core.RuleType.GreaterThan,
                            not = false,
                            overlap = 0.0,
                            enableRule = false,
                            reference = new AdminApiClient.MatchmakerAdminClient.JsonObjectSpecialized("\"value\""),
                            externalData = new Core.RuleExternalData()
                            {
                                cloudSave = new Core.RuleExternalData.CloudSave()
                                {
                                    accessClass = Core.RuleExternalData.CloudSave.AccessClass.Private,
                                    _default = new AdminApiClient.MatchmakerAdminClient.JsonObjectSpecialized(
                                        JsonSampleConfigLoader.WindowsLineEnding("{\n  \"myObject\": \"defaultValue\"\n}"))
                                }
                            },
                            relaxations = new List<Core.RuleRelaxation>()
                        },
                        new Core.Rule()
                        {
                            name = "LeaderboardTiers",
                            source = "ExternalData.Leaderboard.Tiers",
                            type = Core.RuleType.GreaterThanEqual,
                            not = false,
                            overlap = 0.0,
                            enableRule = false,
                            reference = new AdminApiClient.MatchmakerAdminClient.JsonObjectSpecialized("156"),
                            externalData = new Core.RuleExternalData()
                            {
                                leaderboard = new Core.RuleExternalData.Leaderboard()
                                {
                                    id = "MyLeaderboardId"
                                }
                            },
                            relaxations = new List<Core.RuleRelaxation>()
                        },
                        new Core.Rule()
                        {
                            name = "LessThan",
                            source = "attribute",
                            type = Core.RuleType.LessThan,
                            overlap = 0.0,
                            enableRule = false,
                            not = false,
                            reference = new AdminApiClient.MatchmakerAdminClient.JsonObjectSpecialized("2"),
                            relaxations = new List<Core.RuleRelaxation>()
                            {
                                new Core.RuleRelaxation()
                                {
                                    ageType = Core.AgeType.Youngest,
                                    atSeconds = 2,
                                    type = Core.RuleRelaxationType.RuleControlDisable,
                                },
                                new Core.RuleRelaxation()
                                {
                                    ageType = Core.AgeType.Average,
                                    atSeconds = 4,
                                    type = Core.RuleRelaxationType.RuleControlEnable,
                                    value = new AdminApiClient.MatchmakerAdminClient.JsonObjectSpecialized("2"),
                                }
                            },
                            externalData = new Core.RuleExternalData()
                            {
                                cloudSave = new Core.RuleExternalData.CloudSave()
                                {
                                    accessClass = Core.RuleExternalData.CloudSave.AccessClass.Public,
                                    _default = new AdminApiClient.MatchmakerAdminClient.JsonObjectSpecialized("3")
                                }
                            },
                        },
                        new Core.Rule()
                        {
                            name = "LessThanEqual",
                            source = "attribute",
                            type = Core.RuleType.LessThanEqual,
                            overlap = 0.0,
                            enableRule = false,
                            not = false,
                            reference = new AdminApiClient.MatchmakerAdminClient.JsonObjectSpecialized("2"),
                            externalData = new Core.RuleExternalData()
                            {
                                cloudSave = new Core.RuleExternalData.CloudSave()
                                {
                                    accessClass = Core.RuleExternalData.CloudSave.AccessClass.Protected,
                                    _default = new AdminApiClient.MatchmakerAdminClient.JsonObjectSpecialized("3")
                                }
                            },
                            relaxations = new List<Core.RuleRelaxation>()
                        },
                        new Core.Rule()
                        {
                            name = "Equality",
                            source = "attribute",
                            type = Core.RuleType.Equality,
                            overlap = 0.0,
                            enableRule = false,
                            not = false,
                            reference = new AdminApiClient.MatchmakerAdminClient.JsonObjectSpecialized("2"),
                            relaxations = new List<Core.RuleRelaxation>()
                        },
                        new Core.Rule()
                        {
                            name = "InList",
                            source = "attribute",
                            type = Core.RuleType.InList,
                            overlap = 0.0,
                            enableRule = false,
                            not = false,
                            reference = new AdminApiClient.MatchmakerAdminClient.JsonObjectSpecialized(JsonSampleConfigLoader.WindowsLineEnding("[\n  2,\n  3\n]")),
                            relaxations = new List<Core.RuleRelaxation>()
                        },
                        new Core.Rule()
                        {
                            name = "Intersection",
                            source = "attribute",
                            type = Core.RuleType.Intersection,
                            overlap = 0.0,
                            enableRule = false,
                            not = false,
                            reference = new AdminApiClient.MatchmakerAdminClient.JsonObjectSpecialized(JsonSampleConfigLoader.WindowsLineEnding("[\n  2,\n  3\n]")),
                            relaxations = new List<Core.RuleRelaxation>()
                        },
                    }
                }
            },
            Variants = new List<Core.PoolConfig>()
            {
                new Core.PoolConfig()
                {
                    Name = new Core.PoolName("VariantOfDefault"),
                    Enabled = true,
                    TimeoutSeconds = 15,
                    MatchHosting = new Core.MatchIdConfig(),
                    MatchLogic = new Core.MatchLogicRulesConfig()
                    {
                        Name = "VariantMatchLogic",
                        BackfillEnabled = true,
                        MatchDefinition = new Core.RuleBasedMatchDefinition()
                        {
                            teams = new List<Core.RuleBasedTeamDefinition>()
                            {
                                new Core.RuleBasedTeamDefinition()
                                {
                                    name = "rule",
                                    teamCount = new Core.Range()
                                    {
                                        min = 2,
                                        max = 2,
                                    },
                                    playerCount = new Core.Range()
                                    {
                                        min = 1,
                                        max = 10,
                                    }
                                }
                            }
                        }
                    }
                }
            }
        },
        FilteredPools = new List<Core.FilteredPoolConfig>()
        {
            new Core.FilteredPoolConfig()
            {
                Filters = new List<Core.FilteredPoolConfig.Filter>()
                {
                    new Core.FilteredPoolConfig.Filter()
                    {
                        Attribute = "Game-mode-eq",
                        Value = new Core.FilteredPoolConfig.Filter.FilterValue("TDM"),
                        Operator = Core.FilteredPoolConfig.Filter.FilterOperator.Equal,
                    },
                    new Core.FilteredPoolConfig.Filter()
                    {
                        Attribute = "Game-mode-number-lt",
                        Value = new Core.FilteredPoolConfig.Filter.FilterValue(10.5f),
                        Operator = Core.FilteredPoolConfig.Filter.FilterOperator.LessThan,
                    },
                    new Core.FilteredPoolConfig.Filter()
                    {
                        Attribute = "Game-mode-number-gt",
                        Value = new Core.FilteredPoolConfig.Filter.FilterValue(10.5f),
                        Operator = Core.FilteredPoolConfig.Filter.FilterOperator.GreaterThan,
                    },
                    new Core.FilteredPoolConfig.Filter()
                    {
                        Attribute = "Game-mode-number-ne",
                        Value = new Core.FilteredPoolConfig.Filter.FilterValue(10.5f),
                        Operator = Core.FilteredPoolConfig.Filter.FilterOperator.NotEqual,
                    }
                },
                Name = new Core.PoolName("FilteredPool"),
                Enabled = false,
                TimeoutSeconds = 0,
                MatchHosting = new Core.MatchIdConfig(),
                MatchLogic = new Core.MatchLogicRulesConfig()
                {
                    Name = "TestFilteredMatchLogic",
                    MatchDefinition = new Core.RuleBasedMatchDefinition()
                    {
                        teams = new List<Core.RuleBasedTeamDefinition>()
                        {
                            new Core.RuleBasedTeamDefinition()
                            {
                                name = "matchSize",
                                teamCount = new Core.Range()
                                {
                                    min = 2,
                                    max = 2,
                                    relaxations = new List<Core.RangeRelaxation>()
                                    {
                                        new Core.RangeRelaxation()
                                        {
                                            ageType = Core.AgeType.Youngest,
                                            type = Core.RangeRelaxationType.RangeControlReplaceMin,
                                            value = 1,
                                            atSeconds = 23,
                                        }
                                    }
                                },
                                playerCount = new Core.Range()
                                {
                                    min = 7,
                                    max = 10,
                                    relaxations = new List<Core.RangeRelaxation>()
                                    {
                                        new Core.RangeRelaxation()
                                        {
                                            ageType = Core.AgeType.Oldest,
                                            type = Core.RangeRelaxationType.RangeControlReplaceMin,
                                            value = 2.0,
                                            atSeconds = 30.0,
                                        }
                                    }
                                },
                                teamRules = new List<Core.Rule>()
                                {
                                    new Core.Rule()
                                    {
                                        type = Core.RuleType.LessThan,
                                        source = "QoS.Latency",
                                        name = "Latency",
                                        reference = new AdminApiClient.MatchmakerAdminClient.JsonObjectSpecialized("250.7"),
                                        enableRule = false,
                                        not = false
                                    }
                                }
                            }
                        }
                    },
                    BackfillEnabled = true
                },
                Variants = new List<Core.PoolConfig>()
                {
                    new Core.PoolConfig()
                    {
                        Name = new Core.PoolName("VariantPool"),
                        Enabled = false,
                        TimeoutSeconds = 5,
                        MatchHosting = new Core.MatchIdConfig(),
                        MatchLogic = new Core.MatchLogicRulesConfig()
                        {
                            Name = "logic",
                            BackfillEnabled = false,
                            MatchDefinition = new Core.RuleBasedMatchDefinition()
                            {
                                matchRules = new List<Core.Rule>(),
                                teams = new List<Core.RuleBasedTeamDefinition>()
                            }
                        }
                    }
                }
            },
            // Additional CloudCode filtered pool
            new Core.FilteredPoolConfig()
            {
                Filters = new List<Core.FilteredPoolConfig.Filter>()
                {
                    new Core.FilteredPoolConfig.Filter()
                    {
                        Attribute = "Game-mode-eq",
                        Value = new Core.FilteredPoolConfig.Filter.FilterValue("TDM"),
                        Operator = Core.FilteredPoolConfig.Filter.FilterOperator.Equal,
                    }
                },
                Name = new Core.PoolName("CloudCodeFilteredPool"),
                Enabled = false,
                TimeoutSeconds = 0,
                MatchHosting = new Core.CloudCodeConfig()
                {
                    ModuleName = "cc-module",
                    AllocateFunctionName = "cc-allocate",
                    PollFunctionName = "cc-poll"
                },
                MatchLogic = new Core.MatchLogicRulesConfig()
                {
                    Name = "CCFilteredMatchLogic",
                    BackfillEnabled = false,
                    MatchDefinition = new Core.RuleBasedMatchDefinition()
                    {
                        matchRules = new List<Core.Rule>(),
                        teams = new List<Core.RuleBasedTeamDefinition>()
                    }
                },
                Variants = new List<Core.PoolConfig>()
                {
                    new Core.PoolConfig()
                    {
                        Name = new Core.PoolName("CCVariantPool"),
                        Enabled = true,
                        TimeoutSeconds = 10,
                        MatchHosting = new Core.MatchIdConfig(),
                        MatchLogic = new Core.MatchLogicRulesConfig()
                        {
                            Name = "cc-logic",
                            BackfillEnabled = true,
                            MatchDefinition = new Core.RuleBasedMatchDefinition()
                            {
                                matchRules = new List<Core.Rule>(),
                                teams = new List<Core.RuleBasedTeamDefinition>()
                            }
                        }
                    }
                }
            }
        }
    };

    internal Core.QueueConfig EmptyQueueConfig = new Core.QueueConfig()
    {
        Name = new Core.QueueName("EmptyQueue"),
        Enabled = true,
        MaxPlayersPerTicket = 2,
        FilteredPools = new List<Core.FilteredPoolConfig>()
    };

    internal readonly Core.EnvironmentConfig EnvironmentConfig = new Core.EnvironmentConfig()
    {
        Enabled = true,
        DefaultQueueName = new Core.QueueName("DefaultQueueTest"),
    };
}
