using System.Net;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using WireMock.Admin.Mappings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Unity.Services.Cli.MockServer.ServiceMocks.RemoteConfig;

public class RemoteConfigMock : IServiceApiMock
{
    public const string ConfigId = "config-id";

    public const string ConfigTypeDefaultValue = "settings";
    const string k_RemoteConfigPath = "/remote-config/v1";

    readonly string UpdateConfigUrl;
    readonly string GetAllConfigsUrl;
    readonly string DeleteConfigUrl;

    readonly string m_ProjectId;
    readonly string m_EnvironmentId;

    GetResponse m_GetResponse;
    Config m_Config;

    public RemoteConfigMock()
    {
        m_ProjectId = CommonKeys.ValidProjectId;
        m_EnvironmentId = CommonKeys.ValidEnvironmentId;
        UpdateConfigUrl = $"{k_RemoteConfigPath}/projects/{m_ProjectId}/configs";
        GetAllConfigsUrl = $"{k_RemoteConfigPath}/projects/{m_ProjectId}/environments/{m_EnvironmentId}/configs";
        DeleteConfigUrl = $"{k_RemoteConfigPath}/projects/{m_ProjectId}/configs";
        m_Config = new() {
            ProjectId = m_ProjectId,
            EnvironmentId = m_EnvironmentId,
            Type = ConfigTypeDefaultValue,
            Version = "b9c3b33e-0000-4afe-0000-7584ca2dc0cb",
            CreatedAt = "2022-11-14T18:58:19Z",
            UpdatedAt = "2022-11-30T22:16:00Z",
            Value = new List<RemoteConfigEntry>()
            {
                new RemoteConfigEntry() {key = "test", type ="string", value = "west"}
            }
        };
        m_GetResponse = new GetResponse
        {
            Configs = new List<Config> { m_Config }
        };
    }

    public Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        IReadOnlyList<MappingModel> models = new List<MappingModel>();
        return Task.FromResult(models);
    }

    public void CustomMock(WireMockServer mockServer)
    {
        MockListCloudCodeScriptEmpty(mockServer);
        MockGetAllConfigsFromEnvironmentAsync(mockServer, ConfigId);
        MockUpdateConfigAsync(mockServer, ConfigId);
        MockDeleteConfigAsync(mockServer, ConfigId);
    }

    void MockListCloudCodeScriptEmpty(WireMockServer mockServer)
    {
        var response = new ListScriptsResponse(
            new List<ListScriptsResponseResultsInner>
            {
            },
            new ListScriptsResponseLinks("links")
        );

        mockServer.Given(Request.Create().WithPath("*/cloud-code/*/scripts").UsingGet())
            .RespondWith(Response.Create().WithHeaders(new Dictionary<string, string>
            {
                {
                    "Content-Type", "application/json"
                }
            }).WithBodyAsJson(response).WithStatusCode(HttpStatusCode.OK));
    }

    public void MockGetAllConfigsFromEnvironmentAsync(WireMockServer mockServer, string configId)
    {
        m_Config.Id = configId;
        mockServer.Given(Request.Create().WithPath(GetAllConfigsUrl).UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> {{"Content-Type","application/json"}})
                .WithBodyAsJson(m_GetResponse)
                .WithStatusCode(HttpStatusCode.OK));
    }

    public void MockUpdateConfigAsync(WireMockServer mockServer, string configId)
    {
        mockServer
            .Given(Request.Create().WithPath($"{UpdateConfigUrl}/{configId}").UsingPut())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.NoContent));
    }

    public void MockDeleteConfigAsync(WireMockServer mockServer, string configId)
    {
        mockServer
            .Given(Request.Create().WithPath($"{DeleteConfigUrl}/{configId}").UsingDelete())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.NoContent));
    }
}
