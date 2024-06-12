using System.ComponentModel;
using Newtonsoft.Json;
using Core = Unity.Services.Matchmaker.Authoring.Core.Model;
using Generated = Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Model;

namespace Unity.Services.Cli.Matchmaker.AdminApiClient;

static class ModelGeneratedToCore
{
    static Core.Rule FromGeneratedRule(Generated.Rule rule)
    {
        Core.RuleType type = rule.Type switch
        {
            Generated.Rule.TypeEnum.GreaterThan => Core.RuleType.GreaterThan,
            Generated.Rule.TypeEnum.GreaterThanEqual => Core.RuleType.GreaterThanEqual,
            Generated.Rule.TypeEnum.LessThan => Core.RuleType.LessThan,
            Generated.Rule.TypeEnum.LessThanEqual => Core.RuleType.LessThanEqual,
            Generated.Rule.TypeEnum.Difference => Core.RuleType.Difference,
            Generated.Rule.TypeEnum.DoubleDifference => Core.RuleType.DoubleDifference,
            Generated.Rule.TypeEnum.Equality => Core.RuleType.Equality,
            Generated.Rule.TypeEnum.InList => Core.RuleType.InList,
            Generated.Rule.TypeEnum.Intersection => Core.RuleType.Intersection,
            _ => throw new InvalidEnumArgumentException(nameof(type))
        };

        Core.RuleExternalData.CloudSave.AccessClass accessClass = rule.ExternalData?.CloudSave?.AccessClass switch
        {
            Generated.RuleExternalDataCloudSave.AccessClassEnum.Public => Core.RuleExternalData.CloudSave.AccessClass.Public,
            Generated.RuleExternalDataCloudSave.AccessClassEnum.Private => Core.RuleExternalData.CloudSave.AccessClass.Private,
            Generated.RuleExternalDataCloudSave.AccessClassEnum.Protected => Core.RuleExternalData.CloudSave.AccessClass.Protected,
            _ => Core.RuleExternalData.CloudSave.AccessClass.Default,
        };

        Core.RuleExternalData? externalData = null;
        if (rule.ExternalData != null)
        {
            externalData = new Core.RuleExternalData();

            if (rule.ExternalData.Leaderboard != null)
            {
                externalData.leaderboard = new Core.RuleExternalData.Leaderboard
                {
                    id = rule.ExternalData.Leaderboard.Id
                };
            }

            if (rule.ExternalData.CloudSave != null)
            {
                externalData.cloudSave = new Core.RuleExternalData.CloudSave()
                {
                    accessClass = accessClass,
                    _default = new MatchmakerAdminClient.JsonObjectSpecialized(
                        JsonConvert.SerializeObject(rule.ExternalData.CloudSave.Default))
                };
            }
        }
        return new Core.Rule
        {
            source = rule.Source,
            name = rule.Name,
            type = type,
            enableRule = rule.EnableRule,
            not = rule.Not,
            reference = new MatchmakerAdminClient.JsonObjectSpecialized(JsonConvert.SerializeObject(rule.Reference)),
            overlap = Decimal.ToDouble(rule.Overlap),
            relaxations = rule.Relaxations?.Select(
                    rlx =>
                    {
                        Core.RuleRelaxationType rlxType = rlx.Type switch
                        {
                            Generated.RuleRelaxation.TypeEnum.ReferenceControlReplace => Core.RuleRelaxationType.ReferenceControlReplace,
                            Generated.RuleRelaxation.TypeEnum.RuleControlDisable => Core.RuleRelaxationType.RuleControlDisable,
                            Generated.RuleRelaxation.TypeEnum.RuleControlEnable => Core.RuleRelaxationType.RuleControlEnable,
                            _ => throw new InvalidEnumArgumentException(nameof(rlxType))
                        };

                        var coreRuleRelaxation = new Core.RuleRelaxation
                        {
                            type = rlxType,
                            atSeconds = rlx.AtSeconds,
                            ageType = FromGeneratedAgeType(rlx.AgeType)
                        };

                        if (rlx.Value != null)
                        {
                            coreRuleRelaxation.value = new MatchmakerAdminClient.JsonObjectSpecialized(JsonConvert.SerializeObject(rlx.Value));
                        }

                        return coreRuleRelaxation;
                    })
                .ToList() ?? new List<Core.RuleRelaxation>(),
            externalData = externalData
        };
    }

    static Core.RangeRelaxation FromGeneratedRangeRelaxation(Generated.RangeRelaxation rangeRelaxation)
    {
        return new Core.RangeRelaxation()
        {
            type = Core.RangeRelaxationType.RangeControlReplaceMin,
            atSeconds = rangeRelaxation.AtSeconds,
            ageType = FromGeneratedAgeType(rangeRelaxation.AgeType),
            value = rangeRelaxation.Value
        };
    }

    static Core.AgeType FromGeneratedAgeType(Generated.AgeType ageType)
    {
        return ageType switch
        {
            Generated.AgeType.Average => Core.AgeType.Average,
            Generated.AgeType.Oldest => Core.AgeType.Oldest,
            Generated.AgeType.Youngest => Core.AgeType.Youngest,
            _ => throw new InvalidEnumArgumentException(nameof(ageType))
        };
    }

    static Core.RuleBasedMatchDefinition FromGeneratedRuleBase(Generated.RuleBasedMatchDefinition matchDefinition)
    {
        return new Core.RuleBasedMatchDefinition
        {
            matchRules = matchDefinition.MatchRules?.Select(FromGeneratedRule).ToList() ?? new List<Core.Rule>(),
            teams = matchDefinition.Teams?.Select(
                    team => new Core.RuleBasedTeamDefinition
                    {
                        name = team.Name,
                        playerCount = new Core.Range
                        {
                            min = team.PlayerCount.Min,
                            max = team.PlayerCount.Max,
                            relaxations = team.PlayerCount.Relaxations?.Select(FromGeneratedRangeRelaxation).ToList() ?? new List<Core.RangeRelaxation>()
                        },
                        teamCount = new Core.Range
                        {
                            min = team.TeamCount.Min,
                            max = team.TeamCount.Max,
                            relaxations = team.TeamCount.Relaxations?.Select(FromGeneratedRangeRelaxation).ToList() ?? new List<Core.RangeRelaxation>()
                        },
                        teamRules = team.TeamRules?.Select(FromGeneratedRule).ToList() ?? new List<Core.Rule>()
                    })
                .ToList() ?? new List<Core.RuleBasedTeamDefinition>()
        };
    }

    static Core.MatchLogicRulesConfig FromGeneratedMatchLogic(Generated.Rules matchLogic)
    {
        return new Core.MatchLogicRulesConfig
        {
            Name = matchLogic.Name,
            BackfillEnabled = matchLogic.BackfillEnabled,
            MatchDefinition = FromGeneratedRuleBase(matchLogic.MatchDefinition)
        };
    }

    static (Core.IMatchHostingConfig, List<Core.ErrorResponse>) FromGeneratedHostingConfig(Generated.MatchHosting matchHosting, Core.MultiplayResources availableMultiplayResources)
    {
        Core.IMatchHostingConfig matchHostingConfig;
        var errors = new List<Core.ErrorResponse>();
        var buildName = string.Empty;
        var regionName = string.Empty;
        if (matchHosting.ActualInstance is Generated.MultiplayHostingConfig multiplayConfig)
        {
            var fleet = availableMultiplayResources.Fleets.Find(f => f.Id == multiplayConfig.FleetId);
            if (fleet.Name == null)
            {
                errors.Add(
                    new Core.ErrorResponse()
                    {
                        ResultCode = "InvalidMultiplayFleetId",
                        Message = $"Fleet with id '{multiplayConfig.FleetId}' not found."
                    });
            }
            else
            {
                regionName = fleet.QosRegions.Find(r => r.Id == multiplayConfig.DefaultQoSRegionId).Name;
                buildName = fleet.BuildConfigs.Find(b => b.Id == multiplayConfig.BuildConfigurationId).Name;
                if (buildName == null)
                {
                    errors.Add(
                        new Core.ErrorResponse()
                        {
                            ResultCode = "InvalidBuildConfigurationId",
                            Message = $"Build configuration with id '{multiplayConfig.BuildConfigurationId}' not found in fleet named '{fleet.Name}'."
                        });
                }
                else if (regionName == null)
                {
                    errors.Add(
                        new Core.ErrorResponse()
                        {
                            ResultCode = "InvalidDefaultQoSRegion",
                            Message = $"QoS region named '{multiplayConfig.DefaultQoSRegionId}' not found for fleet named '{fleet.Name}'."
                        });
                }
            }
            matchHostingConfig = new Core.MultiplayConfig
            {
                Type = Core.IMatchHostingConfig.MatchHostingType.Multiplay,
                FleetName = fleet.Name,
                BuildConfigurationName = buildName,
                DefaultQoSRegionName = regionName
            };
        }
        else
        {
            matchHostingConfig = new Core.MatchIdConfig()
            {
                Type = Core.IMatchHostingConfig.MatchHostingType.MatchId
            };
        }

        return (matchHostingConfig, errors);
    }

    internal static (Core.QueueConfig, List<Core.ErrorResponse>) FromGeneratedQueueConfig(Generated.QueueConfig queueConfig, Core.MultiplayResources availableMultiplayResources)
    {
        var (defaultPool, errors) = FromGeneratedBasePoolConfig(queueConfig.DefaultPool, availableMultiplayResources);
        var filteredPools = queueConfig.FilteredPools?.Select(v => FromGeneratedFilteredPoolConfig(v, availableMultiplayResources)).ToList();
        errors.AddRange(filteredPools?.Select(v => v.Item2).SelectMany(e => e) ?? new List<Core.ErrorResponse>());
        return (new Core.QueueConfig
        {
            Name = new Core.QueueName(queueConfig.Name),
            Enabled = queueConfig.Enabled,
            MaxPlayersPerTicket = queueConfig.MaxPlayersPerTicket,
            DefaultPool = defaultPool,
            FilteredPools = filteredPools?.Select(v => v.Item1).ToList() ?? new List<Core.FilteredPoolConfig>(),
        }, errors);
    }

    static (Core.BasePoolConfig?, List<Core.ErrorResponse>) FromGeneratedBasePoolConfig(Generated.BasePoolConfig? poolConfig, Core.MultiplayResources availableMultiplayResources)
    {
        if (poolConfig == null)
        {
            return (null, new List<Core.ErrorResponse>());
        }
        var (matchHosting, errors) = FromGeneratedHostingConfig(poolConfig.MatchHosting, availableMultiplayResources);
        var variants = poolConfig.Variants?.Select(v => FromGeneratedPoolConfig(v, availableMultiplayResources)).ToList();
        errors.AddRange(variants?.Select(v => v.Item2).SelectMany(e => e) ?? new List<Core.ErrorResponse>());
        return (new Core.BasePoolConfig
        {
            Name = new Core.PoolName(poolConfig.Name),
            Enabled = poolConfig.Enabled,
            MatchLogic = FromGeneratedMatchLogic(poolConfig.MatchLogic),
            MatchHosting = matchHosting,
            TimeoutSeconds = poolConfig.TimeoutSeconds,
            Variants = variants?.Select(v => v.Item1).ToList() ?? new List<Core.PoolConfig>(),
        }, errors);
    }

    static (Core.PoolConfig, List<Core.ErrorResponse>) FromGeneratedPoolConfig(Generated.PoolConfig poolConfig, Core.MultiplayResources availableMultiplayResources)
    {
        var (matchHosting, errors) = FromGeneratedHostingConfig(poolConfig.MatchHosting, availableMultiplayResources);
        return (new Core.PoolConfig
        {
            Name = new Core.PoolName(poolConfig.Name),
            Enabled = poolConfig.Enabled,
            MatchLogic = FromGeneratedMatchLogic(poolConfig.MatchLogic),
            MatchHosting = matchHosting,
            TimeoutSeconds = poolConfig.TimeoutSeconds
        }, errors);
    }

    static (Core.FilteredPoolConfig, List<Core.ErrorResponse>) FromGeneratedFilteredPoolConfig(Generated.FilteredPoolConfig poolConfig, Core.MultiplayResources availableMultiplayResources)
    {
        var (matchHosting, errors) = FromGeneratedHostingConfig(poolConfig.MatchHosting, availableMultiplayResources);
        var variants = poolConfig.Variants?.Select(v => FromGeneratedPoolConfig(v, availableMultiplayResources)).ToList();
        errors.AddRange(variants?.Select(v => v.Item2).SelectMany(e => e) ?? new List<Core.ErrorResponse>());
        return (new Core.FilteredPoolConfig
        {
            Name = new Core.PoolName(poolConfig.Name),
            Enabled = poolConfig.Enabled,
            MatchLogic = FromGeneratedMatchLogic(poolConfig.MatchLogic),
            MatchHosting = matchHosting,
            TimeoutSeconds = poolConfig.TimeoutSeconds,
            Variants = variants?.Select(v => v.Item1).ToList() ?? new List<Core.PoolConfig>(),
            Filters = poolConfig.Filters?.Select(
                    f =>
                    {
                        Core.FilteredPoolConfig.Filter.FilterOperator filter = f.Operator switch
                        {
                            Generated.Filter.OperatorEnum.GreaterThan => Core.FilteredPoolConfig.Filter.FilterOperator.GreaterThan,
                            Generated.Filter.OperatorEnum.LessThan => Core.FilteredPoolConfig.Filter.FilterOperator.LessThan,
                            Generated.Filter.OperatorEnum.NotEqual => Core.FilteredPoolConfig.Filter.FilterOperator.NotEqual,
                            Generated.Filter.OperatorEnum.Equal => Core.FilteredPoolConfig.Filter.FilterOperator.Equal,
                            _ => throw new InvalidEnumArgumentException(nameof(filter))
                        };

                        return new Core.FilteredPoolConfig.Filter
                        {
                            Attribute = f.Attribute,
                            Operator = filter,
                            Value = new MatchmakerAdminClient.JsonObjectSpecialized(JsonConvert.SerializeObject(f.Value))
                        };
                    })
                .ToList() ?? new List<Core.FilteredPoolConfig.Filter>()
        }, errors);
    }
}
