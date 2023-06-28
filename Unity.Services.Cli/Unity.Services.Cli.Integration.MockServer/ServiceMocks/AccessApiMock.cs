using System.Net;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Gateway.AccessApiV1.Generated.Model;
using WireMock.Admin.Mappings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Unity.Services.Cli.MockServer.ServiceMocks;

public class AccessApiMock : IServiceApiMock
{
    readonly string m_ProjectId;
    readonly string m_EnvironmentId;
    readonly string m_PlayerId;
    readonly string k_AccessModulePath = "/access/v1";

    readonly string projectPolicyUrl;
    readonly string playerPolicyUrl;
    readonly string allPlayerPoliciesUrl;

    public const string PlayerId = "j0oM0dnufzxgtwGqoH0zIpSyWUV7XUgy";

    public AccessApiMock()
    {
        m_ProjectId = CommonKeys.ValidProjectId;
        m_EnvironmentId = CommonKeys.ValidEnvironmentId;
        m_PlayerId = PlayerId;
        projectPolicyUrl = $"{k_AccessModulePath}/projects/{m_ProjectId}/environments/{m_EnvironmentId}/resource-policy";
        playerPolicyUrl =
            $"{k_AccessModulePath}/projects/{m_ProjectId}/environments/{m_EnvironmentId}/players/{m_PlayerId}/resource-policy";
        allPlayerPoliciesUrl = $"{k_AccessModulePath}/projects/{m_ProjectId}/environments/{m_EnvironmentId}/players/resource-policy";
    }

    static Policy GetPolicy()
    {
        List<Statement> statementLists = new List<Statement>();
        var policy = new Policy(statementLists);

        return policy;
    }
    PlayerPolicy GetPlayerPolicy()
    {
        List<Statement> statementLists = new List<Statement>();
        var policy = new PlayerPolicy(playerId: m_PlayerId, statementLists);

        return policy;
    }

    PlayerPolicies GetPlayerPolicies()
    {
        List<PlayerPolicy> results = new List<PlayerPolicy>();
        results.Add(GetPlayerPolicy());

        PlayerPolicies playerPolicies = new PlayerPolicies(next: null, results: results);

        return playerPolicies;
    }

    public void CustomMock(WireMockServer mockServer)
    {
        MockGetProjectPolicy(mockServer);
        MockGetPlayerPolicy(mockServer);
        MockGetAllPlayerPolicies(mockServer);
        MockUpsertProjectPolicy(mockServer);
        MockUpsertPlayerPolicy(mockServer);
        MockDeleteProjectPolicyStatements(mockServer);
        MockDeletePlayerPolicyStatements(mockServer);
    }

    void MockGetProjectPolicy(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create().WithPath(projectPolicyUrl).UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(GetPolicy())
                .WithStatusCode(HttpStatusCode.OK));
    }

    void MockGetPlayerPolicy(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create().WithPath(playerPolicyUrl).UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(GetPlayerPolicy())
                .WithStatusCode(HttpStatusCode.OK));
    }

    void MockGetAllPlayerPolicies(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create().WithPath(allPlayerPoliciesUrl).UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(GetPlayerPolicies())
                .WithStatusCode(HttpStatusCode.OK));
    }

    void MockUpsertProjectPolicy(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create().WithPath(projectPolicyUrl).UsingPatch())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NoContent));
    }

    void MockUpsertPlayerPolicy(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create().WithPath(playerPolicyUrl).UsingPatch())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NoContent));
    }

    void MockDeleteProjectPolicyStatements(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create().WithPath($"{projectPolicyUrl}:delete-statements").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NoContent));
    }

    void MockDeletePlayerPolicyStatements(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create().WithPath($"{playerPolicyUrl}:delete-statements").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NoContent));
    }

    public Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        IReadOnlyList<MappingModel> models = new List<MappingModel>();
        return Task.FromResult(models);
    }

}
