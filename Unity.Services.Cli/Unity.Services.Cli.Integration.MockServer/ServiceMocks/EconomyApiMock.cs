using System.Net;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks.RemoteConfig;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using Unity.Services.Gateway.EconomyApiV2.Generated.Model;
using WireMock.Admin.Mappings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Unity.Services.Cli.MockServer.ServiceMocks;

public class EconomyApiMock : IServiceApiMock
{
    const string k_EconomyOpenApiUrl = "https://services.docs.unity.com/specs/v2/65636f6e6f6d792d61646d696e.yaml";
    public const string ValidFileName = "validname";

    public static CurrencyItemResponse Currency = new CurrencyItemResponse(
        "GOLD_COIN",
        "Gold Coin",
        CurrencyItemResponse.TypeEnum.CURRENCY,
        0,
        100,
        "custom data",
        new ModifiedMetadata(new DateTime(2023, 1, 1)),
        new ModifiedMetadata(new DateTime(2023, 1, 1))
    );

    public async Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        var economyServiceModels =
            await MappingModelUtils.ParseMappingModelsAsync(k_EconomyOpenApiUrl, new());
        economyServiceModels = economyServiceModels.Select(
            m => m.ConfigMappingPathWithKey(CommonKeys.ProjectIdKey, CommonKeys.ValidProjectId)
                .ConfigMappingPathWithKey(CommonKeys.EnvironmentIdKey, CommonKeys.ValidEnvironmentId));
        return economyServiceModels.ToArray();
    }

    static void MockListCloudCodeScriptEmpty(WireMockServer mockServer)
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

        var responseModules = new ListModulesResponse(
            new List<ListModulesResponseResultsInner> { },
            ""
        );

        mockServer?.Given(Request.Create().WithPath("*/cloud-code/*/modules").UsingGet())
            .RespondWith(Response.Create().WithHeaders(new Dictionary<string, string>
            {
                {
                    "Content-Type", "application/json"
                }
            }).WithBodyAsJson(responseModules).WithStatusCode(HttpStatusCode.OK));
    }

    static void MockList(WireMockServer mockServer)
    {
        var response = new List<GetResourcesResponseResultsInner>
        {
            new GetResourcesResponseResultsInner(Currency)
        };

        GetResourcesResponse responseOut = new GetResourcesResponse(response);

        mockServer.Given(Request.Create()
                .WithPath("*/economy/v2/projects/*/environments/*/configs/draft/resources")
                .UsingGet())
        .RespondWith(Response.Create()
            .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
            .WithBodyAsJson(responseOut)
            .WithStatusCode(HttpStatusCode.OK));
    }

    public void CustomMock(WireMockServer mockServer)
    {
        MockList(mockServer);
        MockListCloudCodeScriptEmpty(mockServer);
        MockListRemoteConfigEmpty(mockServer);
        mockServer.AllowPartialMapping();
    }

    public static void MockListRemoteConfigEmpty(WireMockServer mockServer)
    {
        var getResponse = new GetResponse
        {
            Configs = new List<Config> { }
        };
        mockServer.Given(Request.Create()
                .WithPath("/remote-config/v1/projects/*environments/*configs")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(getResponse)
                .WithStatusCode(HttpStatusCode.OK));
    }
}
