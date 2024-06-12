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
    const string k_AccessModulePath = "/access/resource-policy/v1";

    readonly string m_ProjectPolicyUrl;
    readonly string m_PlayerPolicyUrl;
    readonly string m_AllPlayerPoliciesUrl;

    public const string PlayerId = "j0oM0dnufzxgtwGqoH0zIpSyWUV7XUgy";

    public AccessApiMock()
    {
        m_ProjectId = CommonKeys.ValidProjectId;
        m_EnvironmentId = CommonKeys.ValidEnvironmentId;
        m_PlayerId = PlayerId;
        m_ProjectPolicyUrl = $"{k_AccessModulePath}/projects/{m_ProjectId}/environments/{m_EnvironmentId}/resource-policy";
        m_PlayerPolicyUrl =
            $"{k_AccessModulePath}/projects/{m_ProjectId}/environments/{m_EnvironmentId}/players/{m_PlayerId}/resource-policy";
        m_AllPlayerPoliciesUrl = $"{k_AccessModulePath}/projects/{m_ProjectId}/environments/{m_EnvironmentId}/players/resource-policy";
    }

    static Policy GetPolicy()
    {
        var statement = new ProjectStatement(
            "statement-1",
            new List<string>()
            {
                "*"
            },
            "Deny",
            "Player",
            "urn:ugs:*");
        List<ProjectStatement> statementLists = new List<ProjectStatement>() { statement };
        var policy = new Policy(statementLists);

        return policy;
    }
    PlayerPolicy GetPlayerPolicy()
    {
        List<PlayerStatement> statementLists = new List<PlayerStatement>();
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
        mockServer.Given(Request.Create().WithPath(m_ProjectPolicyUrl).UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(GetPolicy())
                .WithStatusCode(HttpStatusCode.OK));
    }

    void MockGetPlayerPolicy(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create().WithPath(m_PlayerPolicyUrl).UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(GetPlayerPolicy())
                .WithStatusCode(HttpStatusCode.OK));
    }

    void MockGetAllPlayerPolicies(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create().WithPath(m_AllPlayerPoliciesUrl).UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(GetPlayerPolicies())
                .WithStatusCode(HttpStatusCode.OK));
    }

    void MockUpsertProjectPolicy(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create().WithPath(m_ProjectPolicyUrl).UsingPatch())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NoContent));
    }

    void MockUpsertPlayerPolicy(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create().WithPath(m_PlayerPolicyUrl).UsingPatch())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NoContent));
    }

    void MockDeleteProjectPolicyStatements(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create().WithPath($"{m_ProjectPolicyUrl}:delete-statements").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NoContent));
    }

    void MockDeletePlayerPolicyStatements(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create().WithPath($"{m_PlayerPolicyUrl}:delete-statements").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NoContent));
    }

    public Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        IReadOnlyList<MappingModel> models = new List<MappingModel>();
        return Task.FromResult(models);
    }

}
