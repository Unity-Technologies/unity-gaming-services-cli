using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.MockServer;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Client;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Unity.Services.Cli.IntegrationTest.LeaderboardTests;

public class LeaderboardApiMock
{
    const string k_LeaderboardPath = "/leaderboards/v1beta1";
    readonly string BaseUrl;

    readonly Dictionary<string, string> RequestHeader = new ()
    {
        { "Content-Type", "application/json" }
    };

    public MockApi MockApi { get; }
    public LeaderboardApiMock(string projectId, string environmentId, MockApi? mockApi = null) {
        if (mockApi == null)
        {
            MockApi = new(NetworkTargetEndpoints.MockServer);
        }
        else
        {
            MockApi = mockApi;
        }

        BaseUrl = $"{MockApi.Server?.Url}{k_LeaderboardPath}/projects/{projectId}/environments/{environmentId}/leaderboards";
    }

    public void MockListLeaderboards(List<UpdatedLeaderboardConfig> leaderboardConfigs, HttpStatusCode code = HttpStatusCode.OK)
    {
        var response = new LeaderboardConfigPage()
        {
            Results = leaderboardConfigs
        };

        MockApi.Server?.Given(Request.Create().WithPath(BaseUrl).UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(RequestHeader)
                .WithBodyAsJson(response)
                .WithStatusCode(code));
    }

    public void MockGetLeaderboard(UpdatedLeaderboardConfig leaderboardConfig, HttpStatusCode code = HttpStatusCode.OK)
    {
        MockApi.Server?.Given(Request.Create().WithPath(BaseUrl + "/" + leaderboardConfig.Id).UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(RequestHeader)
                .WithBodyAsJson(leaderboardConfig)
                .WithStatusCode(code));
    }

    public void MockCreateLeaderboard(UpdatedLeaderboardConfig leaderboardConfig, HttpStatusCode code = HttpStatusCode.Created)
    {
        MockApi.Server?.Given(Request.Create().WithPath(BaseUrl).UsingPost())
            .RespondWith(Response.Create()
                .WithHeaders(RequestHeader)
                .WithBodyAsJson(leaderboardConfig)
                .WithStatusCode(code));
    }

    public void MockUpdateLeaderboard(UpdatedLeaderboardConfig leaderboardConfig, HttpStatusCode code = HttpStatusCode.NoContent)
    {
        MockApi.Server?.Given(Request.Create().WithPath(BaseUrl + "/" + leaderboardConfig.Id).UsingPatch())
            .RespondWith(Response.Create()
                .WithHeaders(RequestHeader)
                .WithBodyAsJson(leaderboardConfig)
                .WithStatusCode(code));
    }

    public void MockDeleteLeaderboard(string leaderboardId, HttpStatusCode code = HttpStatusCode.NoContent)
    {
        MockApi.Server?.Given(Request.Create().WithPath(BaseUrl + "/" + leaderboardId).UsingDelete())
            .RespondWith(Response.Create()
                .WithHeaders(RequestHeader)
                .WithStatusCode(code));
    }

    public void MockResetLeaderboard(string leaderboardId, string versionId, HttpStatusCode code = HttpStatusCode.OK)
    {
        MockApi.Server?.Given(Request.Create().WithPath(BaseUrl+ "/" + leaderboardId + "/scores").UsingDelete())
            .RespondWith(Response.Create()
                .WithHeaders(RequestHeader)
                .WithBodyAsJson(new LeaderboardVersionId(versionId))
                .WithStatusCode(code));
    }

    public void MockImportLeaderboard(UpdatedLeaderboardConfig leaderboardConfig, HttpStatusCode code = HttpStatusCode.NoContent)
    {
        MockListLeaderboards(new List<UpdatedLeaderboardConfig>(){leaderboardConfig}, code);
        MockGetLeaderboard(leaderboardConfig, code);
        MockUpdateLeaderboard(leaderboardConfig, code);
    }

    public void MockExportLeaderboard(UpdatedLeaderboardConfig leaderboardConfig, HttpStatusCode code = HttpStatusCode.OK)
    {
        MockListLeaderboards(new(){leaderboardConfig}, code);
        MockGetLeaderboard(leaderboardConfig, code);
    }
}
