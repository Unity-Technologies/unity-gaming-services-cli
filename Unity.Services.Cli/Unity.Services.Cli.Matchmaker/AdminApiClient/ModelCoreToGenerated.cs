using System.ComponentModel;
using System.Globalization;
using Core = Unity.Services.Matchmaker.Authoring.Core.Model;
using Generated = Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Model;

namespace Unity.Services.Cli.Matchmaker.AdminApiClient;

static class ModelCoreToGenerated
{

    internal static (Generated.QueueConfig, List<Core.ErrorResponse>) FromCoreQueueConfig(
        Core.QueueConfig queueConfig,
        Core.MultiplayResources availableMultiplayResources,
        bool dryRun)
    {
        var (defaultPool, errorResponses) = FromCoreBasePoolConfig(
            queueConfig.DefaultPool,
            availableMultiplayResources,
            dryRun);

        var filteredPools = queueConfig.FilteredPools?.Select(f => FromCoreFilteredPoolConfig(f, availableMultiplayResources, dryRun)).ToList();
        errorResponses.AddRange(filteredPools?.Select(f => f.Item2).SelectMany(f => f) ?? new List<Core.ErrorResponse>());

        return (
            new Generated.QueueConfig(
                name: queueConfig.Name.ToString() ?? string.Empty,
                enabled: queueConfig.Enabled,
                maxPlayersPerTicket: queueConfig.MaxPlayersPerTicket,
                defaultPool: defaultPool,
                filteredPools: filteredPools?.Select(f => f.Item1).ToList() ?? new List<Generated.FilteredPoolConfig>()
            ), errorResponses);
    }

    static (Generated.MatchHosting, List<Core.ErrorResponse>) FromCoreMatchHosting(
        Core.IMatchHostingConfig matchHostingConfig,
        Core.MultiplayResources availableMultiplayResources,
        bool dryRun)
    {
        var errors = new List<Core.ErrorResponse>();
        if (matchHostingConfig is Core.MultiplayConfig multiplayConfig)
        {
            var fleet = availableMultiplayResources.Fleets.Find(f => f.Name == multiplayConfig.FleetName);
            if (fleet.Name == null)
            {
                errors.Add(
                    new Core.ErrorResponse()
                    {
                        ResultCode = "InvalidMultiplayFleetName",
                        Message = $"Fleet named '{multiplayConfig.FleetName}' not found."
                    });
            }
            else
            {
                var qosRegion = fleet.QosRegions.Find(r => r.Name == multiplayConfig.DefaultQoSRegionName);
                var buildConfig = fleet.BuildConfigs.Find(b => b.Name == multiplayConfig.BuildConfigurationName);
                if (buildConfig.Name == null)
                {
                    errors.Add(
                        new Core.ErrorResponse()
                        {
                            ResultCode = "InvalidBuildConfigurationName",
                            Message = $"Build configuration named '{multiplayConfig.BuildConfigurationName}' not found in fleet named '{multiplayConfig.FleetName}'."
                        });
                }
                else if (qosRegion.Name == null)
                {
                    errors.Add(
                        new Core.ErrorResponse()
                        {
                            ResultCode = "InvalidDefaultQoSRegion",
                            Message = $"QoS region named '{multiplayConfig.DefaultQoSRegionName}' not found for fleet named '{multiplayConfig.FleetName}'."
                        });
                }
                else
                {
                    if (!dryRun)
                    {
                        return (new Generated.MatchHosting(
                            new Generated.MultiplayHostingConfig(
                                type: Generated.MultiplayHostingConfig.TypeEnum.Multiplay,
                                fleetId: fleet.Id,
                                buildConfigurationId: buildConfig.Id,
                                defaultQoSRegionId: qosRegion.Id
                            )), errors);
                    }
                }
            }
        }
        return (new Generated.MatchHosting(new Generated.MatchIdHostingConfig(type: Generated.MatchIdHostingConfig.TypeEnum.MatchId)), errors);
    }

    static Generated.Rule FromCoreRule(Core.Rule rule)
    {
        Generated.Rule.TypeEnum type = rule.type switch
        {
            Core.RuleType.GreaterThan => Generated.Rule.TypeEnum.GreaterThan,
            Core.RuleType.GreaterThanEqual => Generated.Rule.TypeEnum.GreaterThanEqual,
            Core.RuleType.LessThan => Generated.Rule.TypeEnum.LessThan,
            Core.RuleType.LessThanEqual => Generated.Rule.TypeEnum.LessThanEqual,
            Core.RuleType.Difference => Generated.Rule.TypeEnum.Difference,
            Core.RuleType.DoubleDifference => Generated.Rule.TypeEnum.DoubleDifference,
            Core.RuleType.Equality => Generated.Rule.TypeEnum.Equality,
            Core.RuleType.InList => Generated.Rule.TypeEnum.InList,
            Core.RuleType.Intersection => Generated.Rule.TypeEnum.Intersection,
            _ => throw new InvalidEnumArgumentException(nameof(type))
        };

        Generated.RuleExternalDataCloudSave.AccessClassEnum accessClass =
            rule.externalData?.cloudSave?.accessClass switch
            {
                Core.RuleExternalData.CloudSave.AccessClass.Public => Generated.RuleExternalDataCloudSave
                    .AccessClassEnum.Public,
                Core.RuleExternalData.CloudSave.AccessClass.Private => Generated.RuleExternalDataCloudSave
                    .AccessClassEnum.Private,
                Core.RuleExternalData.CloudSave.AccessClass.Protected => Generated.RuleExternalDataCloudSave
                    .AccessClassEnum.Protected,
                _ => Generated.RuleExternalDataCloudSave.AccessClassEnum.Default,
            };

        var generatedRule = new Generated.Rule(
            source: rule.source,
            name: rule.name,
            type: type,
            enableRule: rule.enableRule,
            not: rule.not,
            reference: rule.reference,
            overlap: Convert.ToDecimal(rule.overlap, CultureInfo.InvariantCulture),
            relaxations: rule.relaxations?.Select(FromCoreRuleRelaxation).ToList() ??
                         new List<Generated.RuleRelaxation>()
        );

        if (rule.externalData != null)
        {
            generatedRule.ExternalData = new Generated.RuleExternalData();
            if (rule.externalData.leaderboard != null)
            {
                generatedRule.ExternalData.Leaderboard =
                    new Generated.RuleExternalDataLeaderboard(rule.externalData.leaderboard.id);
            }

            if (rule.externalData.cloudSave != null)
            {
                generatedRule.ExternalData.CloudSave = new Generated.RuleExternalDataCloudSave(
                    _default: rule.externalData.cloudSave._default,
                    accessClass: accessClass);
            }
        }

        return generatedRule;
    }

    static Generated.AgeType FromCoreAgeType(Core.AgeType ageType)
    {
        return ageType switch
        {
            Core.AgeType.Average => Generated.AgeType.Average,
            Core.AgeType.Oldest => Generated.AgeType.Oldest,
            Core.AgeType.Youngest => Generated.AgeType.Youngest,
            _ => throw new InvalidEnumArgumentException(nameof(ageType))
        };
    }

    static Generated.RuleRelaxation FromCoreRuleRelaxation(Core.RuleRelaxation ruleRelaxation)
    {
        Generated.RuleRelaxation.TypeEnum type = ruleRelaxation.type switch
        {
            Core.RuleRelaxationType.ReferenceControlReplace =>
                Generated.RuleRelaxation.TypeEnum.ReferenceControlReplace,
            Core.RuleRelaxationType.RuleControlDisable => Generated.RuleRelaxation.TypeEnum.RuleControlDisable,
            Core.RuleRelaxationType.RuleControlEnable => Generated.RuleRelaxation.TypeEnum.RuleControlEnable,
            _ => throw new InvalidEnumArgumentException(nameof(type))
        };

        return new Generated.RuleRelaxation(
            type: type,
            atSeconds: ruleRelaxation.atSeconds,
            ageType: FromCoreAgeType(ruleRelaxation.ageType),
            value: ruleRelaxation.value
        );
    }

    static Generated.RangeRelaxation FromCoreRangeRelaxation(Core.RangeRelaxation rangeRelaxation)
    {
        return new Generated.RangeRelaxation(
            type: Generated.RangeRelaxation.TypeEnum.RangeControlReplaceMin,
            atSeconds: rangeRelaxation.atSeconds,
            ageType: FromCoreAgeType(rangeRelaxation.ageType),
            value: rangeRelaxation.value
        );
    }

    static Generated.RuleBasedMatchDefinition FromCoreRuleBasedMatchDefinition(
        Core.RuleBasedMatchDefinition ruleBasedMatchDefinition)
    {
        return new Generated.RuleBasedMatchDefinition(
            teams: ruleBasedMatchDefinition.teams?.Select(
                    team => new Generated.RuleBasedTeamDefinition(
                        name: team.name,
                        teamCount: new Generated.Range(
                            min: team.teamCount.min,
                            max: team.teamCount.max,
                            relaxations: team.teamCount.relaxations?.Select(FromCoreRangeRelaxation).ToList() ??
                                         new List<Generated.RangeRelaxation>()
                        ),
                        playerCount: new Generated.Range(
                            min: team.playerCount.min,
                            max: team.playerCount.max,
                            relaxations: team.playerCount.relaxations?.Select(FromCoreRangeRelaxation).ToList() ??
                                         new List<Generated.RangeRelaxation>()
                        ),
                        teamRules: team.teamRules?.Select(FromCoreRule).ToList() ?? new List<Generated.Rule>())
                )
                .ToList() ?? new List<Generated.RuleBasedTeamDefinition>(),
            matchRules: ruleBasedMatchDefinition.matchRules?.Select(FromCoreRule).ToList() ??
                        new List<Generated.Rule>()
        );
    }

    static Generated.Rules FromCoreMatchLogic(Core.MatchLogicRulesConfig matchRules)
    {
        return new Generated.Rules(
            name: matchRules.Name,
            backfillEnabled: matchRules.BackfillEnabled,
            matchDefinition: FromCoreRuleBasedMatchDefinition(matchRules.MatchDefinition)
        );
    }

    static (Generated.PoolConfig, List<Core.ErrorResponse>) FromCorePoolConfig(
        Core.PoolConfig poolConfig,
        Core.MultiplayResources availableMultiplayResources,
        bool dryRun)
    {
        var matchHosting = FromCoreMatchHosting(poolConfig.MatchHosting, availableMultiplayResources, dryRun);
        return (new Generated.PoolConfig(
            name: poolConfig.Name.ToString() ?? string.Empty,
            enabled: poolConfig.Enabled,
            timeoutSeconds: poolConfig.TimeoutSeconds,
            matchLogic: FromCoreMatchLogic(poolConfig.MatchLogic),
            matchHosting: matchHosting.Item1
        ), matchHosting.Item2);
    }

    static (Generated.FilteredPoolConfig, List<Core.ErrorResponse>) FromCoreFilteredPoolConfig(
        Core.FilteredPoolConfig poolConfig,
        Core.MultiplayResources availableMultiplayResources,
        bool dryRun)
    {
        var (matchHosting, errors) = FromCoreMatchHosting(
            poolConfig.MatchHosting,
            availableMultiplayResources,
            dryRun);

        var variants = poolConfig.Variants?.Select(p => FromCorePoolConfig(p, availableMultiplayResources, dryRun)).ToList();
        errors.AddRange(variants?.Select(v => v.Item2).SelectMany(e => e).ToList() ?? new List<Core.ErrorResponse>());

        return (new Generated.FilteredPoolConfig(
            name: poolConfig.Name.ToString() ?? string.Empty,
            enabled: poolConfig.Enabled,
            timeoutSeconds: poolConfig.TimeoutSeconds,
            matchLogic: FromCoreMatchLogic(poolConfig.MatchLogic),
            matchHosting: matchHosting,
            variants: variants?.Select(v => v.Item1).ToList(),
            filters: poolConfig.Filters?.Select(
                    f =>
                    {
                        Generated.Filter.OperatorEnum filter = f.Operator switch
                        {
                            Core.FilteredPoolConfig.Filter.FilterOperator.GreaterThan => Generated.Filter
                                .OperatorEnum
                                .GreaterThan,
                            Core.FilteredPoolConfig.Filter.FilterOperator.LessThan => Generated.Filter.OperatorEnum
                                .LessThan,
                            Core.FilteredPoolConfig.Filter.FilterOperator.NotEqual => Generated.Filter.OperatorEnum
                                .NotEqual,
                            Core.FilteredPoolConfig.Filter.FilterOperator.Equal => Generated.Filter.OperatorEnum
                                .Equal,
                            _ => throw new InvalidEnumArgumentException(nameof(f.Operator))
                        };

                        return new Generated.Filter(
                            attribute: f.Attribute,
                            _operator: filter,
                            value: f.Value
                        );
                    })
                .ToList() ?? new List<Generated.Filter>()
        ), errors);
    }

    static (Generated.BasePoolConfig?, List<Core.ErrorResponse>) FromCoreBasePoolConfig(
        Core.BasePoolConfig? poolConfig,
        Core.MultiplayResources availableMultiplayResources,
        bool dryRun)
    {
        if (poolConfig == null)
        {
            return (null, new List<Core.ErrorResponse>());
        }
        var (matchHosting, errors) = FromCoreMatchHosting(
            poolConfig.MatchHosting,
            availableMultiplayResources,
            dryRun);

        var variants = poolConfig.Variants?.Select(p => FromCorePoolConfig(p, availableMultiplayResources, dryRun)).ToList();
        errors.AddRange(variants?.Select(v => v.Item2).SelectMany(e => e).ToList() ?? new List<Core.ErrorResponse>());

        return (new Generated.BasePoolConfig(
            name: poolConfig.Name.ToString() ?? string.Empty,
            enabled: poolConfig.Enabled,
            timeoutSeconds: poolConfig.TimeoutSeconds,
            matchLogic: FromCoreMatchLogic(poolConfig.MatchLogic),
            matchHosting: matchHosting,
            variants: variants?.Select(v => v.Item1).ToList() ?? new List<Generated.PoolConfig>()
        ), errors);
    }
}

