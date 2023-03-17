using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Unity.Services.Cli.MockServer;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using WireMock.Admin.Mappings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Unity.Services.Cli.IntegrationTest.CloudCodeTests;

public static class CloudCodeV1MockServerModels
{
    const string k_CloudCodeV1Config = "cloud-code-api-v1-generator-config.yaml";

    public static async Task<IEnumerable<MappingModel>> GetModels(string validScriptName = "test-script")
    {
        var models = await MappingModelUtils.ParseMappingModelsFromGeneratorConfigAsync(k_CloudCodeV1Config, new());
        return models.Select(
            m => m.ConfigMappingPathWithKey(CommonKeys.ProjectIdKey, CommonKeys.ValidProjectId)
                .ConfigMappingPathWithKey(CommonKeys.EnvironmentIdKey, CommonKeys.ValidEnvironmentId)
                .ConfigMappingPathWithKey("scripts", validScriptName));
    }

    public static async Task<IEnumerable<MappingModel>> GetModuleModels(string validModuleName = "test_module")
    {
        var models = await MappingModelUtils.ParseMappingModelsFromGeneratorConfigAsync(k_CloudCodeV1Config, new());
        return models.Select(
            m => m.ConfigMappingPathWithKey(CommonKeys.ProjectIdKey, CommonKeys.ValidProjectId)
                .ConfigMappingPathWithKey(CommonKeys.EnvironmentIdKey, CommonKeys.ValidEnvironmentId)
                .ConfigMappingPathWithKey("modules", validModuleName));
    }

    // OverrideListModulesAsync maps the modules list endpoint to a set response
    // as default wiremock responses can trigger infinite loops via page tokens.
    public static void OverrideListModules(MockApi mock)
    {
        var response = new ListModulesResponse(
            new List<ListModulesResponseResultsInner>
            {
                new(
                    "ExistingModule",
                    Language.CS
                ),
                new(
                    "AnotherExistingModule",
                    Language.CS
                )
            },
            ""
        );

        mock.Server?.Given(Request.Create().WithPath("*/cloud-code/*/modules").UsingGet())
            .RespondWith(Response.Create().WithHeaders(new Dictionary<string, string>
            {
                {
                    "Content-Type", "application/json"
                }
            }).WithBodyAsJson(response).WithStatusCode(HttpStatusCode.OK));
    }
}
