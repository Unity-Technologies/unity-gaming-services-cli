using Newtonsoft.Json;
using Unity.Services.Cli.Leaderboards.Service;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Client;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;
using Unity.Services.Leaderboards.Authoring.Core.Model;
using Unity.Services.Leaderboards.Authoring.Core.Service;
using CoreSortOrder = Unity.Services.Leaderboards.Authoring.Core.Model.SortOrder;
using CoreUpdateType = Unity.Services.Leaderboards.Authoring.Core.Model.UpdateType;
using CoreResetConfig = Unity.Services.Leaderboards.Authoring.Core.Model.ResetConfig;
using CoreTieringConfig = Unity.Services.Leaderboards.Authoring.Core.Model.TieringConfig;
using CoreStrategy = Unity.Services.Leaderboards.Authoring.Core.Model.Strategy;
using ApiResetConfig = Unity.Services.Gateway.LeaderboardApiV1.Generated.Model.ResetConfig;
using ApiSortOrder = Unity.Services.Gateway.LeaderboardApiV1.Generated.Model.SortOrder;
using ApiTieringConfig = Unity.Services.Gateway.LeaderboardApiV1.Generated.Model.TieringConfig;
using ApiUpdateType = Unity.Services.Gateway.LeaderboardApiV1.Generated.Model.UpdateType;
using ApiStrategy = Unity.Services.Gateway.LeaderboardApiV1.Generated.Model.TieringConfig.StrategyEnum;
using LeaderboardConfig = Unity.Services.Leaderboards.Authoring.Core.Model.LeaderboardConfig;

namespace Unity.Services.Cli.Leaderboards.Deploy;

class LeaderboardsClient : ILeaderboardsClient
{
    readonly ILeaderboardsService m_LeaderboardsService;
    internal string ProjectId { get; set; }
    internal string EnvironmentId { get; set; }
    internal CancellationToken CancellationToken { get; set; }

    public LeaderboardsClient(
        ILeaderboardsService service,
        string projectId = "",
        string environmentId = "",
        CancellationToken cancellationToken = default)
    {
        m_LeaderboardsService = service;
        ProjectId = projectId;
        EnvironmentId = environmentId;
        CancellationToken = cancellationToken;
    }

    public void Initialize(string environmentId, string projectId, CancellationToken cancellationToken)
    {
        EnvironmentId = environmentId;
        ProjectId = projectId;
        CancellationToken = cancellationToken;
    }

    public async Task<ILeaderboardConfig> Get(string id, CancellationToken token)
    {
        ApiResponse<UpdatedLeaderboardConfig> response = await m_LeaderboardsService.GetLeaderboardAsync(
            ProjectId,
            EnvironmentId,
            id,
            token);
        return FromResponse(response.Data);
    }

    public async Task Update(ILeaderboardConfig leaderboardConfig, CancellationToken token)
    {
        await m_LeaderboardsService.UpdateLeaderboardAsync(
            ProjectId,
            EnvironmentId,
            leaderboardConfig.Id,
            PatchFromConfig(leaderboardConfig),
            token);
    }

    public async Task Create(ILeaderboardConfig leaderboardConfig, CancellationToken token)
    {
        await m_LeaderboardsService.CreateLeaderboardAsync(
            ProjectId,
            EnvironmentId,
            CreateFromConfig(leaderboardConfig),
            token);
    }

    public async Task Delete(ILeaderboardConfig leaderboardConfig, CancellationToken token)
    {
        await m_LeaderboardsService.DeleteLeaderboardAsync(
            ProjectId,
            EnvironmentId,
            leaderboardConfig.Id,
            token);
    }

    public async Task<IReadOnlyList<ILeaderboardConfig>> List(CancellationToken token)
    {
        const int limit = 50;
        var leaderboards = new List<ILeaderboardConfig>();
        string? cursor = null;
        List<UpdatedLeaderboardConfig> newBatch;
        do
        {
            var rawResponse = await m_LeaderboardsService.GetLeaderboardsAsync(
                ProjectId,
                EnvironmentId,
                cursor: cursor,
                limit: limit,
                cancellationToken: token);

            newBatch = rawResponse.ToList();
            cursor = newBatch.LastOrDefault()?.Id;
            leaderboards.AddRange(newBatch.Select(FromResponse));

            if (token.IsCancellationRequested)
                break;
        } while (newBatch.Count >= limit);

        return leaderboards;
    }

    static ILeaderboardConfig FromResponse(UpdatedLeaderboardConfig responseData)
    {
        var lb = new LeaderboardConfig(
            responseData.Id,
            responseData.Name,
            (CoreSortOrder)(int)responseData.SortOrder,
            (CoreUpdateType)(int)responseData.UpdateType);

        lb.BucketSize = responseData.BucketSize;
        lb.ResetConfig = FromResponse(responseData.ResetConfig);
        lb.TieringConfig = FromResponse(responseData.TieringConfig);
        lb.Path = "Remote";
        return lb;
    }

    static CoreResetConfig? FromResponse(ApiResetConfig? resetConfig)
    {
        if (resetConfig == null)
            return null;

        return new CoreResetConfig()
        {
            Archive = resetConfig.Archive,
            Schedule = resetConfig.Schedule,
            Start = resetConfig.Start
        };
    }

    static CoreTieringConfig? FromResponse(ApiTieringConfig? tieringConfig)
    {
        if (tieringConfig == null)
            return null;

        return new CoreTieringConfig()
        {
           Strategy = (Strategy)(int)tieringConfig.Strategy,
           Tiers = FromResponse(tieringConfig.Tiers)
        };
    }

    static List<Tier> FromResponse(List<TieringConfigTiersInner> tiers)
    {
        return tiers
            .Select(
                t => new Tier()
                {
                    Cutoff = t.Cutoff,
                    Id = t.Id
                })
            .ToList();
    }

    static LeaderboardIdConfig CreateFromConfig(ILeaderboardConfig leaderboardConfig)
    {
        return new LeaderboardIdConfig(leaderboardConfig.Id, leaderboardConfig.Name)
        {
            SortOrder = (ApiSortOrder)(int)leaderboardConfig.SortOrder,
            UpdateType = (ApiUpdateType)(int)leaderboardConfig.UpdateType,
            TieringConfig = FromConfig(leaderboardConfig.TieringConfig),
            ResetConfig = FromConfig(leaderboardConfig.ResetConfig),
            BucketSize = leaderboardConfig.BucketSize
        };
    }


    static LeaderboardPatchConfig PatchFromConfig(ILeaderboardConfig leaderboardConfig)
    {
        var res = new LeaderboardPatchSpecializedConfig()
        {
            Name = leaderboardConfig.Name,
            SortOrder = (ApiSortOrder)(int)leaderboardConfig.SortOrder,
            UpdateType = (ApiUpdateType)(int)leaderboardConfig.UpdateType,
            TieringConfig = FromConfig(leaderboardConfig.TieringConfig),
            ResetConfig = FromConfig(leaderboardConfig.ResetConfig)
        };
        return res;
    }

    static ApiTieringConfig? FromConfig(CoreTieringConfig? config)
    {
        if (config == null)
            return null;
        return new ApiTieringConfig(
            tiers:FromConfig(config.Tiers),
            strategy: (ApiStrategy)(int)config.Strategy);
    }

    static List<TieringConfigTiersInner> FromConfig(List<Tier> tiers)
    {
        return tiers
            .Select(
                t => new TieringConfigTiersInner(t.Id)
                {
                    Cutoff = t.Cutoff
                })
            .ToList();
    }

    static ApiResetConfig? FromConfig(CoreResetConfig? config)
    {
        if (config == null)
            return null;

        return new ApiResetConfig()
        {
            Archive = config.Archive,
            Schedule = config.Schedule,
            Start = config.Start
        };
    }

    [JsonConverter(typeof(LeaderboardPatchConverter))]
    class LeaderboardPatchSpecializedConfig : LeaderboardPatchConfig { }
}
