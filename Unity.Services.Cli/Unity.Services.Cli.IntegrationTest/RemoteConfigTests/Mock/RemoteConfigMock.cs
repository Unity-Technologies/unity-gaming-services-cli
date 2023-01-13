using System.Collections.Generic;
using System.Net;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.MockServer;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Unity.Services.Cli.IntegrationTest.RemoteConfigTests.Mock;

public class RemoteConfigMock
{
    public const string ConfigTypeDefaultValue = "settings";
    const string k_RemoteConfigPath = "/remote-config/v1";

    readonly string UpdateConfigUrl;
    readonly string GetAllConfigsUrl;

    readonly string m_ProjectId;
    readonly string m_EnvironmentId;
    public MockApi MockApi { get; }
    GetResponse m_GetResponse;
    Config m_Config;
    public RemoteConfigMock(string projectId, string environmentId)
    {
        MockApi = new(NetworkTargetEndpoints.MockServer);
        m_ProjectId = projectId;
        m_EnvironmentId = environmentId;
        UpdateConfigUrl = $"{MockApi.Server?.Url}{k_RemoteConfigPath}/projects/{m_ProjectId}/configs";
        GetAllConfigsUrl = $"{MockApi.Server?.Url}{k_RemoteConfigPath}/projects/{m_ProjectId}/environments/{m_EnvironmentId}/configs";
        m_Config = new() {
            ProjectId = m_ProjectId,
            EnvironmentId = environmentId,
            Type = ConfigTypeDefaultValue,
            Version = "b9c3b33e-0000-4afe-0000-7584ca2dc0cb",
            CreatedAt = "2022-11-14T18:58:19Z",
            UpdatedAt = "2022-11-30T22:16:00Z",
            Value = new List<RemoteConfigEntry>()
        };
        m_GetResponse = new GetResponse
        {
            Configs = new List<Config> { m_Config }
        };
    }

    public void MockGetAllConfigsFromEnvironmentAsync(string configId)
    {
        m_Config.Id = configId;
        MockApi.Server?.Given(Request.Create().WithPath(GetAllConfigsUrl).UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> {{"Content-Type","application/json"}})
                .WithBodyAsJson(m_GetResponse)
                .WithStatusCode(HttpStatusCode.OK));
    }

    public void MockUpdateConfigAsync(string configId)
    {
        MockApi.Server?
            .Given(Request.Create().WithPath($"{UpdateConfigUrl}/{configId}").UsingPut())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.NoContent));
    }
}
