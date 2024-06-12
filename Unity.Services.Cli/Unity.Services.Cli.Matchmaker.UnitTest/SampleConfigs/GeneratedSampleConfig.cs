namespace Unity.Services.Cli.Matchmaker.UnitTest.SampleConfigs;
using Generated = Gateway.MatchmakerAdminApiV3.Generated.Model;

class GeneratedSampleConfig
{
    struct TestTruct
    {
        public string myObject;

        public TestTruct()
        {
            myObject = "defaultValue";
        }

        public TestTruct(string myObject)
        {
            this.myObject = myObject;
        }
    }

    public Generated.QueueConfig QueueConfig
    {
        get => new Generated.QueueConfig(
            name: "DefaultQueueTest",
            enabled: true,
            maxPlayersPerTicket: 1,
            defaultPool: new Generated.BasePoolConfig(
                name: "TestPool",
                enabled: true,
                matchHosting: new Generated.MatchHosting(
                    new Generated.MultiplayHostingConfig(
                        type: Generated.MultiplayHostingConfig.TypeEnum.Multiplay,
                        fleetId: "e8b109e1-6746-4ce6-9c21-3330509554a1",
                        buildConfigurationId: "74874928923749",
                        defaultQoSRegionId: "3eac13c4-bf61-4b05-83df-eed5732ad305"
                    )),
                matchLogic: new Generated.Rules(
                    name: "TestMatchLogic",
                    backfillEnabled: false,
                    matchDefinition: new Generated.RuleBasedMatchDefinition(
                        matchRules: new List<Generated.Rule>()
                        {
                            new Generated.Rule(
                                name: "Skill",
                                source: "Player.Skill",
                                type: Generated.Rule.TypeEnum.Difference,
                                not: true,
                                reference: new[] { "test_string" },
                                enableRule: true,
                                relaxations: new List<Generated.RuleRelaxation>()
                                {
                                    new Generated.RuleRelaxation(
                                        type: Generated.RuleRelaxation.TypeEnum.ReferenceControlReplace,
                                        value: 100,
                                        atSeconds: 30,
                                        ageType: Generated.AgeType.Oldest
                                    ),
                                }
                            ),
                            new Generated.Rule(
                                name: "CloudSaveElo",
                                source: "ExternalData.CloudSave.myObject",
                                type: Generated.Rule.TypeEnum.GreaterThan,
                                not: false,
                                reference: "value",
                                externalData: new Generated.RuleExternalData
                                (
                                    cloudSave: new Generated.RuleExternalDataCloudSave(
                                        accessClass: Generated.RuleExternalDataCloudSave.AccessClassEnum.Private,
                                        _default: new TestTruct()
                                    )
                                ),
                                relaxations: new List<Generated.RuleRelaxation>()
                            ),
                            new Generated.Rule(
                                name: "LeaderboardTiers",
                                source: "ExternalData.Leaderboard.Tiers",
                                type: Generated.Rule.TypeEnum.GreaterThanEqual,
                                not: false,
                                reference: 156,
                                externalData: new Generated.RuleExternalData
                                (
                                    leaderboard: new Generated.RuleExternalDataLeaderboard
                                    (
                                        id: "MyLeaderboardId"
                                    )
                                ),
                                relaxations: new List<Generated.RuleRelaxation>()
                            ),
                            new Generated.Rule(
                                name: "LessThan",
                                source: "attribute",
                                type: Generated.Rule.TypeEnum.LessThan,
                                reference: 2,
                                relaxations: new List<Generated.RuleRelaxation>()
                                {
                                    new Generated.RuleRelaxation(
                                        ageType: Generated.AgeType.Youngest,
                                        atSeconds: 2,
                                        type: Generated.RuleRelaxation.TypeEnum.RuleControlDisable
                                    ),
                                    new Generated.RuleRelaxation(
                                        ageType: Generated.AgeType.Average,
                                        atSeconds: 4,
                                        type: Generated.RuleRelaxation.TypeEnum.RuleControlEnable,
                                        value: 2
                                    ),
                                },
                                externalData: new Generated.RuleExternalData(
                                    cloudSave: new Generated.RuleExternalDataCloudSave(
                                        accessClass: Generated.RuleExternalDataCloudSave.AccessClassEnum.Public,
                                        _default: 3
                                    )
                                )
                            ),
                            new Generated.Rule(
                                name: "LessThanEqual",
                                source: "attribute",
                                type: Generated.Rule.TypeEnum.LessThanEqual,
                                reference: 2,
                                externalData: new Generated.RuleExternalData(
                                    cloudSave: new Generated.RuleExternalDataCloudSave(
                                        accessClass: Generated.RuleExternalDataCloudSave.AccessClassEnum.Protected,
                                        _default: 3
                                    )
                                ),
                                relaxations: new List<Generated.RuleRelaxation>()
                            ),
                            new Generated.Rule(
                                name: "Equality",
                                source: "attribute",
                                type: Generated.Rule.TypeEnum.Equality,
                                reference: 2,
                                relaxations: new List<Generated.RuleRelaxation>()
                            ),
                            new Generated.Rule(
                                name: "InList",
                                source: "attribute",
                                type: Generated.Rule.TypeEnum.InList,
                                reference: new List<int> {2, 3},
                                relaxations: new List<Generated.RuleRelaxation>()
                            ),
                            new Generated.Rule(
                                name: "Intersection",
                                source: "attribute",
                                type: Generated.Rule.TypeEnum.Intersection,
                                reference: new List<int> {2, 3},
                                relaxations: new List<Generated.RuleRelaxation>()
                            ),
                        },
                        teams: new List<Generated.RuleBasedTeamDefinition>()
                    )),
                variants: new List<Generated.PoolConfig>()
                {
                    new Generated.PoolConfig(
                        name: "VariantOfDefault",
                        enabled: true,
                        matchHosting: new Generated.MatchHosting(new Generated.MatchIdHostingConfig(Generated.MatchIdHostingConfig.TypeEnum.MatchId)),
                        timeoutSeconds: 15,
                        new Generated.Rules(
                            name: "VariantMatchLogic",
                            backfillEnabled: true,
                            matchDefinition: new Generated.RuleBasedMatchDefinition(
                                matchRules: new List<Generated.Rule>(),
                                teams: new List<Generated.RuleBasedTeamDefinition>()
                                {
                                    new (
                                        name:"rule",
                                        teamCount: new Generated.Range(min:2, max:2,  relaxations: new List<Generated.RangeRelaxation>()),
                                        playerCount: new Generated.Range(1, 10, relaxations: new List<Generated.RangeRelaxation>()),
                                        teamRules: new List<Generated.Rule>()
                                        )
                                }
                            )
                        )
                    )
                }
            ),
            filteredPools: new List<Generated.FilteredPoolConfig>()
            {
                new Generated.FilteredPoolConfig(
                    name: "FilteredPool",
                    enabled: false,
                    filters: new List<Generated.Filter>()
                    {
                        new Generated.Filter(
                            attribute: "Game-mode-eq",
                            value: "TDM",
                            _operator: Generated.Filter.OperatorEnum.Equal
                        ),
                        new Generated.Filter(
                            attribute: "Game-mode-number-lt",
                            value: 10.5,
                            _operator: Generated.Filter.OperatorEnum.LessThan
                        ),
                        new Generated.Filter(
                            attribute: "Game-mode-number-gt",
                            value: 10.5,
                            _operator: Generated.Filter.OperatorEnum.GreaterThan
                        ),
                        new Generated.Filter(
                            attribute: "Game-mode-number-ne",
                            value: 10.5,
                            _operator: Generated.Filter.OperatorEnum.NotEqual
                        )
                    },
                    matchHosting: new Generated.MatchHosting(
                        new Generated.MatchIdHostingConfig()
                        {
                            Type = Generated.MatchIdHostingConfig.TypeEnum.MatchId
                        }),
                    matchLogic: new Generated.Rules(
                        name: "TestFilteredMatchLogic",
                        backfillEnabled: true,
                        matchDefinition: new Generated.RuleBasedMatchDefinition(
                            teams: new List<Generated.RuleBasedTeamDefinition>()
                            {
                                new Generated.RuleBasedTeamDefinition(
                                    name: "matchSize",
                                    playerCount: new Generated.Range(
                                        max: 10,
                                        min: 7,
                                        relaxations: new List<Generated.RangeRelaxation>()
                                        {
                                            new Generated.RangeRelaxation(
                                                ageType: Generated.AgeType.Oldest,
                                                atSeconds: 30,
                                                type: Generated.RangeRelaxation.TypeEnum.RangeControlReplaceMin,
                                                value: 2
                                            )
                                        }
                                    ),
                                    teamCount: new Generated.Range(
                                        min: 2,
                                        max: 2,
                                        relaxations: new List<Generated.RangeRelaxation>()
                                        {
                                            new Generated.RangeRelaxation(
                                                ageType: Generated.AgeType.Youngest,
                                                atSeconds: 23,
                                                type: Generated.RangeRelaxation.TypeEnum.RangeControlReplaceMin,
                                                value: 1)
                                        }
                                    ),
                                    teamRules: new List<Generated.Rule>()
                                    {
                                        new Generated.Rule(
                                            name: "Latency",
                                            source: "QoS.Latency",
                                            type: Generated.Rule.TypeEnum.LessThan,
                                            not: false,
                                            reference: 250.7,
                                            enableRule: false,
                                            relaxations: new List<Generated.RuleRelaxation>()
                                        ),
                                    }
                                )
                            },
                            matchRules: new List<Generated.Rule>()
                        )
                    ),
                    variants: new List<Generated.PoolConfig>()
                    {
                        new Generated.PoolConfig(
                            name: "VariantPool",
                            enabled: false,
                            timeoutSeconds: 5,
                            matchLogic: new Generated.Rules(
                                name: "logic",
                                backfillEnabled: false,
                                matchDefinition: new Generated.RuleBasedMatchDefinition(
                                    matchRules: new List<Generated.Rule>(),
                                    teams: new List<Generated.RuleBasedTeamDefinition>()
                                )
                            ),
                            matchHosting: new Generated.MatchHosting(
                                new Generated.MatchIdHostingConfig()
                                {
                                    Type = Generated.MatchIdHostingConfig.TypeEnum.MatchId
                                }
                            )
                        )
                    }
                ),
            }
        );
    }

    public Generated.QueueConfig EmptyQueueConfig
    {
        get => new Generated.QueueConfig(
            name: "EmptyQueue",
            enabled: true,
            maxPlayersPerTicket: 2,
            filteredPools: new List<Generated.FilteredPoolConfig>());
    }
}
