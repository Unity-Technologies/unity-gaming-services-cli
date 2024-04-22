using System.Net;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;
using WireMock.Admin.Mappings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Unity.Services.Cli.MockServer.ServiceMocks;

public class LeaderboardApiMock : IServiceApiMock
{
    const string k_LeaderboardPath = "/leaderboards/v1";
    static readonly string k_Leaderboard1VersionId = "v10";

    public static readonly UpdatedLeaderboardConfig Leaderboard1 = new(
        "lb1",
        "leaderboard 1",
        SortOrder.Asc,
        UpdateType.Aggregate,
        10,
        new ResetConfig(new DateTime(2023, 1, 1), "@1d", true),
        new TieringConfig(TieringConfig.StrategyEnum.Percent, new List<TieringConfigTiersInner>() { new TieringConfigTiersInner("tier1", 2) }),
        versions: new List<LeaderboardVersion>()
        );
    public static readonly UpdatedLeaderboardConfig Leaderboard2 = new(
        "lb2",
        "leaderboard 2",
        SortOrder.Asc,
        UpdateType.Aggregate,
        10,
        new ResetConfig(new DateTime(2023, 1, 1), "@1d", true)
    );
    public static readonly UpdatedLeaderboardConfig Leaderboard3 = new(
        "lb3",
        "leaderboard 3",
        SortOrder.Asc,
        UpdateType.Aggregate,
        10,
        new ResetConfig(new DateTime(2023, 1, 1), "@1d", true),
        new TieringConfig(TieringConfig.StrategyEnum.Percent, new List<TieringConfigTiersInner>() { new TieringConfigTiersInner("tier1", 2) }),
        versions: new List<LeaderboardVersion>()
    );
    public static readonly UpdatedLeaderboardConfig Leaderboard4 = new(
        "lb4",
        "leaderboard 4",
        SortOrder.Asc,
        UpdateType.Aggregate,
        10,
        new ResetConfig(new DateTime(2023, 1, 1), "@1d", true),
        new TieringConfig(TieringConfig.StrategyEnum.Percent, new List<TieringConfigTiersInner>() { new TieringConfigTiersInner("tier1", 2) }),
        versions: new List<LeaderboardVersion>()
    );
    public static readonly UpdatedLeaderboardConfig Leaderboard5 = new(
        "lb5",
        "leaderboard 5",
        SortOrder.Asc,
        UpdateType.Aggregate,
        10,
        new ResetConfig(new DateTime(2023, 1, 1), "@1d", true),
        new TieringConfig(TieringConfig.StrategyEnum.Percent, new List<TieringConfigTiersInner>() { new TieringConfigTiersInner("tier1", 2) }),
        versions: new List<LeaderboardVersion>()
    );
    public static readonly UpdatedLeaderboardConfig Leaderboard6 = new(
        "lb6",
        "leaderboard 6",
        SortOrder.Asc,
        UpdateType.Aggregate,
        10,
        new ResetConfig(new DateTime(2023, 1, 1), "@1d", true),
        new TieringConfig(TieringConfig.StrategyEnum.Percent, new List<TieringConfigTiersInner>() { new TieringConfigTiersInner("tier1", 2) }),
        versions: new List<LeaderboardVersion>()
    );
    public static readonly UpdatedLeaderboardConfig Leaderboard7 = new(
        "lb7",
        "leaderboard 7",
        SortOrder.Asc,
        UpdateType.Aggregate,
        10,
        new ResetConfig(new DateTime(2023, 1, 1), "@1d", true),
        new TieringConfig(TieringConfig.StrategyEnum.Percent, new List<TieringConfigTiersInner>() { new TieringConfigTiersInner("tier1", 2) }),
        versions: new List<LeaderboardVersion>()
    );
    public static readonly UpdatedLeaderboardConfig Leaderboard8 = new(
        "lb8",
        "leaderboard 8",
        SortOrder.Asc,
        UpdateType.Aggregate,
        10,
        new ResetConfig(new DateTime(2023, 1, 1), "@1d", true),
        new TieringConfig(TieringConfig.StrategyEnum.Percent, new List<TieringConfigTiersInner>() { new TieringConfigTiersInner("tier1", 2) }),
        versions: new List<LeaderboardVersion>()
    );
    public static readonly UpdatedLeaderboardConfig Leaderboard9 = new(
        "lb9",
        "leaderboard 9",
        SortOrder.Asc,
        UpdateType.Aggregate,
        10,
        new ResetConfig(new DateTime(2023, 1, 1), "@1d", true),
        new TieringConfig(TieringConfig.StrategyEnum.Percent, new List<TieringConfigTiersInner>() { new TieringConfigTiersInner("tier1", 2) }),
        versions: new List<LeaderboardVersion>()
    );
    public static readonly UpdatedLeaderboardConfig Leaderboard10 = new(
        "lb10",
        "leaderboard 10",
        SortOrder.Asc,
        UpdateType.Aggregate,
        10,
        new ResetConfig(new DateTime(2023, 1, 1), "@1d", true),
        new TieringConfig(TieringConfig.StrategyEnum.Percent, new List<TieringConfigTiersInner>() { new TieringConfigTiersInner("tier1", 2) }),
        versions: new List<LeaderboardVersion>()
    );
    public static readonly UpdatedLeaderboardConfig Leaderboard11 = new(
        "lb11",
        "leaderboard 11",
        SortOrder.Asc,
        UpdateType.Aggregate,
        10,
        new ResetConfig(new DateTime(2023, 1, 1), "@1d", true),
        new TieringConfig(TieringConfig.StrategyEnum.Percent, new List<TieringConfigTiersInner>() { new TieringConfigTiersInner("tier1", 2) }),
        versions: new List<LeaderboardVersion>()
    );
    public static readonly UpdatedLeaderboardConfig Leaderboard12 = new(
        "lb12",
        "leaderboard 12",
        SortOrder.Asc,
        UpdateType.Aggregate,
        10,
        new ResetConfig(new DateTime(2023, 1, 1), "@1d", true),
        new TieringConfig(TieringConfig.StrategyEnum.Percent, new List<TieringConfigTiersInner>() { new TieringConfigTiersInner("tier1", 2) }),
        versions: new List<LeaderboardVersion>()
    );

    readonly string m_BaseUrl;

    readonly Dictionary<string, string> m_RequestHeader = new()
    {
        { "Content-Type", "application/json" }
    };

    public LeaderboardApiMock()
    {
        m_BaseUrl = $"{k_LeaderboardPath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/leaderboards";
    }

    public Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        IReadOnlyList<MappingModel> models = new List<MappingModel>();
        return Task.FromResult(models);
    }

    public void CustomMock(WireMockServer mockServer)
    {
        MockListLeaderboards(mockServer, new List<UpdatedLeaderboardConfig>()
        {
            Leaderboard1,
            Leaderboard2
        });

        MockGetLeaderboard(mockServer, Leaderboard1);
        MockDeleteLeaderboard(mockServer, Leaderboard1.Id);
        MockResetLeaderboard(mockServer, Leaderboard1.Id, k_Leaderboard1VersionId);

        MockCreateLeaderboard(mockServer, Leaderboard1);
        MockUpdateLeaderboard(mockServer, Leaderboard1);
    }

    void MockListLeaderboards(WireMockServer mockServer, List<UpdatedLeaderboardConfig> leaderboardConfigs,
        HttpStatusCode code = HttpStatusCode.OK)
    {
        var response = new LeaderboardConfigPage()
        {
            Results = leaderboardConfigs
        };

        mockServer.Given(Request.Create().WithPath(m_BaseUrl).UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(m_RequestHeader)
                .WithBodyAsJson(response)
                .WithStatusCode(code));
    }

    void MockGetLeaderboard(WireMockServer mockServer, UpdatedLeaderboardConfig leaderboardConfig, HttpStatusCode code = HttpStatusCode.OK)
    {
        mockServer.Given(Request.Create().WithPath(m_BaseUrl + "/" + leaderboardConfig.Id).UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(m_RequestHeader)
                .WithBodyAsJson(leaderboardConfig)
                .WithStatusCode(code));
    }

    void MockCreateLeaderboard(WireMockServer mockServer, UpdatedLeaderboardConfig leaderboardConfig, HttpStatusCode code = HttpStatusCode.Created)
    {
        mockServer.Given(Request.Create().WithPath(m_BaseUrl).UsingPost())
            .RespondWith(Response.Create()
                .WithHeaders(m_RequestHeader)
                .WithBodyAsJson(leaderboardConfig)
                .WithStatusCode(code));
    }

    public void MockUpdateLeaderboard(WireMockServer mockServer, UpdatedLeaderboardConfig leaderboardConfig, HttpStatusCode code = HttpStatusCode.NoContent)
    {
        mockServer.Given(Request.Create().WithPath(m_BaseUrl + "/" + leaderboardConfig.Id).UsingPatch())
            .RespondWith(Response.Create()
                .WithHeaders(m_RequestHeader)
                .WithBodyAsJson(leaderboardConfig)
                .WithStatusCode(code));
    }

    void MockDeleteLeaderboard(WireMockServer mockServer, string leaderboardId, HttpStatusCode code = HttpStatusCode.NoContent)
    {
        mockServer.Given(Request.Create().WithPath(m_BaseUrl + "/" + leaderboardId).UsingDelete())
            .RespondWith(Response.Create()
                .WithHeaders(m_RequestHeader)
                .WithStatusCode(code));
    }

    void MockResetLeaderboard(WireMockServer mockServer, string leaderboardId, string versionId, HttpStatusCode code = HttpStatusCode.OK)
    {
        mockServer.Given(Request.Create().WithPath(m_BaseUrl + "/" + leaderboardId + "/scores").UsingDelete())
            .RespondWith(Response.Create()
                .WithHeaders(m_RequestHeader)
                .WithBodyAsJson(new LeaderboardVersionId(versionId))
                .WithStatusCode(code));
    }

}
