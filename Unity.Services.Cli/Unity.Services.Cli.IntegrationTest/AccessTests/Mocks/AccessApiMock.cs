using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.MockServer;
using Unity.Services.Gateway.AccessApiV1.Generated.Model;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Unity.Services.Cli.IntegrationTest.AccessTests.Mock;

public class AccessApiMock
{
    readonly string m_ProjectId;
    readonly string m_EnvironmentId;
    readonly string m_PlayerId;
    private readonly string k_AccessModulePath = "/access/v1";

    private readonly string projectPolicyUrl;
    private readonly string playerPolicyUrl;
    private readonly string allPlayerPoliciesUrl;

    public MockApi MockApi { get; }

    public AccessApiMock(string projectId, string environmentId, string playerId)
    {
        MockApi = new(NetworkTargetEndpoints.MockServer);
        m_ProjectId = projectId;
        m_EnvironmentId = environmentId;
        m_PlayerId = playerId;
        projectPolicyUrl = $"{MockApi.Server?.Url}{k_AccessModulePath}/projects/{m_ProjectId}/environments/{m_EnvironmentId}/resource-policy";
        playerPolicyUrl =
            $"{MockApi.Server?.Url}{k_AccessModulePath}/projects/{m_ProjectId}/environments/{m_EnvironmentId}/players/{m_PlayerId}/resource-policy";
        allPlayerPoliciesUrl = $"{MockApi.Server?.Url}{k_AccessModulePath}/projects/{m_ProjectId}/environments/{m_EnvironmentId}/players/resource-policy";
    }

    private static Policy GetPolicy()
    {
        List<Statement> statementLists = new List<Statement>();
        var policy = new Policy(statementLists);

        return policy;
    }
    private PlayerPolicy GetPlayerPolicy()
    {
        List<Statement> statementLists = new List<Statement>();
        var policy = new PlayerPolicy(playerId: m_PlayerId, statementLists);

        return policy;
    }

    private PlayerPolicies GetPlayerPolicies()
    {
        List<PlayerPolicy> results = new List<PlayerPolicy>();
        results.Add(GetPlayerPolicy());

        PlayerPolicies playerPolicies = new PlayerPolicies(next: null, results: results);

        return playerPolicies;
    }

    public void MockGetProjectPolicy()
    {
        MockApi.Server?.Given(Request.Create().WithPath(projectPolicyUrl).UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> {{"Content-Type","application/json"}})
                .WithBodyAsJson(GetPolicy())
                .WithStatusCode(HttpStatusCode.OK));
    }

    public void MockGetPlayerPolicy()
    {
        MockApi.Server?.Given(Request.Create().WithPath(playerPolicyUrl).UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> {{"Content-Type","application/json"}})
                .WithBodyAsJson(GetPlayerPolicy())
                .WithStatusCode(HttpStatusCode.OK));
    }

    public void MockGetAllPlayerPolicies()
    {
        MockApi.Server?.Given(Request.Create().WithPath(allPlayerPoliciesUrl).UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> {{"Content-Type","application/json"}})
                .WithBodyAsJson(GetPlayerPolicies())
                .WithStatusCode(HttpStatusCode.OK));
    }

    public void MockUpsertProjectPolicy()
    {
        MockApi.Server?.Given(Request.Create().WithPath(projectPolicyUrl).UsingPatch())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NoContent));
    }

    public void MockUpsertPlayerPolicy()
    {
        MockApi.Server?.Given(Request.Create().WithPath(playerPolicyUrl).UsingPatch())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NoContent));
    }

    public void MockDeleteProjectPolicyStatements()
    {
        MockApi.Server?.Given(Request.Create().WithPath($"{projectPolicyUrl}:delete-statements").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NoContent));
    }

    public void MockDeletePlayerPolicyStatements()
    {
        MockApi.Server?.Given(Request.Create().WithPath($"{playerPolicyUrl}:delete-statements").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NoContent));
    }
}
