using System.Net;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks.RemoteConfig;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using Unity.Services.Gateway.EconomyApiV2.Generated.Model;

using WireMock.Admin.Mappings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Unity.Services.Cli.MockServer.ServiceMocks.CloudCode;

public class CloudCodeFetchMock : IServiceApiMock
{
    const string k_CloudCodeV1Config = "cloud-code-api-v1-generator-config.yaml";
    public const string ValidScriptName = "test-3";
    public const string ValidModuleName = "test_3";

    public static readonly List<ListScriptsResponseResultsInner> RemoteScripts =
        new List<ListScriptsResponseResultsInner>
        {
            new(
                "remoteScript1",
                ScriptType.API,
                Language.JS,
                true,
                DateTime.MinValue,
                1),
            new(
                "remoteScript2",
                ScriptType.API,
                Language.JS,
                true,
                DateTime.MinValue,
                1),
            new(
                "remoteScript3",
                ScriptType.API,
                Language.JS,
                true,
                DateTime.MinValue,
                1)
        };
    public async Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        var script1Models = await GetScriptModels("remoteScript1");
        var script2Models = await GetScriptModels("remoteScript2");
        var script3Models = await GetScriptModels("remoteScript3");
        return script1Models.Concat(script2Models).Concat(script3Models).ToArray();
    }

    public void CustomMock(WireMockServer mockServer)
    {
        MockListScript(mockServer);
        MockListRemoteConfigEmpty(mockServer);
        MockListEconomyResourceEmpty(mockServer);
    }

    static void MockListEconomyResourceEmpty(WireMockServer mockServer)
    {
        var publishedResourcesResponse = new GetPublishedResourcesResponse(new List<GetResourcesResponseResultsInner>());
        mockServer.Given(Request.Create().WithPath($"*/economy/v2/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/published/resources").UsingGet())
            .RespondWith(Response.Create().WithHeaders(new Dictionary<string, string>
            {
                {
                    "Content-Type", "application/json"
                }
            }).WithBodyAsJson(publishedResourcesResponse).WithStatusCode(HttpStatusCode.OK));

        var resourcesResponse = new GetResourcesResponse(new List<GetResourcesResponseResultsInner>());
        mockServer.Given(Request.Create().WithPath($"*/economy/v2/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/draft/resources").UsingGet())
            .RespondWith(Response.Create().WithHeaders(new Dictionary<string, string>
            {
                {
                    "Content-Type", "application/json"
                }
            }).WithBodyAsJson(resourcesResponse).WithStatusCode(HttpStatusCode.OK));
    }

    static void MockListRemoteConfigEmpty(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create().WithPath($"*/remote-config/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs").UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(new GetResponse
                {
                    Configs = new List<Config>()
                })
                .WithStatusCode(HttpStatusCode.OK));
    }

    static void MockListScript(WireMockServer mockServer)
    {
        var response = new ListScriptsResponse(
            RemoteScripts,
            new ListScriptsResponseLinks("links")
        );

        mockServer?.Given(Request.Create().WithPath("*/cloud-code/*/scripts").UsingGet())
            .RespondWith(Response.Create().WithHeaders(new Dictionary<string, string>
            {
                {
                    "Content-Type", "application/json"
                }
            }).WithBodyAsJson(response).WithStatusCode(HttpStatusCode.OK));
    }

    static async Task<IEnumerable<MappingModel>> GetScriptModels(string validScriptName)
    {
        var models = await MappingModelUtils.ParseMappingModelsFromGeneratorConfigAsync(k_CloudCodeV1Config, new());
        return models.Select(
            m => m.ConfigMappingPathWithKey(CommonKeys.ProjectIdKey, CommonKeys.ValidProjectId)
                .ConfigMappingPathWithKey(CommonKeys.EnvironmentIdKey, CommonKeys.ValidEnvironmentId)
                .ConfigMappingPathWithKey("scripts", validScriptName));
    }
}
