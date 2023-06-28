using System.Net;
using OpenTelemetry.Trace;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using WireMock.Admin.Mappings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Unity.Services.Cli.MockServer.ServiceMocks.CloudCode;

public class CloudCodeV1Mock : IServiceApiMock
{
    const string k_CloudCodeV1Config = "cloud-code-api-v1-generator-config.yaml";
    public const string ValidScriptName = "test-3";
    public const string ValidModuleName = "test_3";
    const string sampleURL = "https://google.com";

    public async Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        var scriptModels = await GetScriptModels(ValidScriptName);
        var exampleScriptModels = await GetScriptModels("example-string");
        var moduleModels = (await GetModuleModels(ValidModuleName))
            .Concat(await GetModuleModels("ExistingModule"))
            .Concat(await GetModuleModels("AnotherExistingModule"));
        return scriptModels.Concat(exampleScriptModels).Concat(moduleModels).ToArray();
    }

    public void CustomMock(WireMockServer mockServer)
    {
        MockListCSharpModule(mockServer);
        MockHttpClientForModuleDownloads(mockServer);
    }

    static void MockListCSharpModule(WireMockServer mockServer)
    {
        var response = new ListModulesResponse(
            new List<ListModulesResponseResultsInner>
            {
                new(
                    "ExistingModule",
                    Language.CS,
                    null,
                    sampleURL

                ),
                new(
                    "AnotherExistingModule",
                    Language.CS,
                    null,
                    sampleURL
                )
            },
            ""
        );

        mockServer?.Given(Request.Create().WithPath("*/cloud-code/*/modules").UsingGet())
            .RespondWith(Response.Create().WithHeaders(new Dictionary<string, string>
            {
                {
                    "Content-Type", "application/json"
                }
            }).WithBodyAsJson(response).WithStatusCode(HttpStatusCode.OK));
    }

    // Mocks out the file stream response for HTTPClient when testing import/export.
    static void MockHttpClientForModuleDownloads(WireMockServer mockServer)
    {
        const string testFile
            = "../../../../Unity.Services.Cli.CloudCode.UnitTest/ModuleTestCases/test.ccmzip";
        mockServer?.Given(Request.Create().WithUrl(sampleURL).UsingGet())
            .RespondWith(Response.Create().WithHeaders(new Dictionary<string, string>
            {
                {
                    "Content-Type", "application/zip"
                }
            }).WithBodyFromFile(testFile)
                .WithStatusCode(HttpStatusCode.OK));

    }

    static async Task<IEnumerable<MappingModel>> GetScriptModels(string validScriptName = "test-script")
    {
        var models = await MappingModelUtils.ParseMappingModelsFromGeneratorConfigAsync(k_CloudCodeV1Config, new());
        return models.Select(
            m => m.ConfigMappingPathWithKey(CommonKeys.ProjectIdKey, CommonKeys.ValidProjectId)
                .ConfigMappingPathWithKey(CommonKeys.EnvironmentIdKey, CommonKeys.ValidEnvironmentId)
                .ConfigMappingPathWithKey("scripts", validScriptName));
    }

    static async Task<IEnumerable<MappingModel>> GetModuleModels(string validModuleName = "test_module")
    {
        var models = await MappingModelUtils.ParseMappingModelsFromGeneratorConfigAsync(k_CloudCodeV1Config, new());
        return models.Select(
            m => m.ConfigMappingPathWithKey(CommonKeys.ProjectIdKey, CommonKeys.ValidProjectId)
                .ConfigMappingPathWithKey(CommonKeys.EnvironmentIdKey, CommonKeys.ValidEnvironmentId)
                .ConfigMappingPathWithKey("modules", validModuleName));
    }
}
