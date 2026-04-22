global using ReleaseResponse =
    Unity.Services.Gateway.LiveReleasesApiV1.Generated.Model.GetReleasesByProjectID200ResponseInner;
global using PointerResponse =
    Unity.Services.Gateway.LiveReleasesApiV1.Generated.Model.GetReleasePointersByProjectID200ResponseInner;
global using ReleaseResponseInnerPointer =
    Unity.Services.Gateway.LiveReleasesApiV1.Generated.Model.GetReleasesByProjectID200ResponseInnerReleasePointersInner;
global using TargetingRule =
    Unity.Services.Gateway.LiveReleasesApiV1.Generated.Model.GetAllTargetingRules200ResponseInner;
using Unity.Services.Cli.MockServer.Common;
using WireMock.Admin.Mappings;
using WireMock.Matchers;
using WireMock.Net.OpenApiParser.Settings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;

namespace Unity.Services.Cli.MockServer.ServiceMocks;

public class LiveReleasesApiMock : IServiceApiMock
{
    const string k_BasePath = "/v1";

    const string k_ValidEnvironmentId = "00000000-0000-0000-0000-000000000000";

    // Valid Release Pointers Inner
    const string k_PointerWithRefId = "00000000-0000-0000-0000-00000000ptr1";
    const string k_PointerWithRefName = "live";
    const string k_PointerWithRefDescription = "live pointer";

    static readonly ReleaseResponseInnerPointer k_PointerInner = new(
        k_PointerWithRefId,
        k_PointerWithRefName,
        k_PointerWithRefDescription,
        k_ValidEnvironmentId
    );

    // Valid Releases
    const string k_ReleaseWithPointerId = "00000000-0000-0000-0000-00000000rel1";
    const string k_ReleaseWithPointerName = "Release-1";
    const string k_ReleaseWithPointerDescription = "Description1";

    static readonly ReleaseResponse k_ReleaseResponseWithPointer = new(
        k_ReleaseWithPointerId,
        k_ReleaseWithPointerName,
        k_ReleaseWithPointerDescription,
        k_ValidEnvironmentId,
        releasePointers: [k_PointerInner]
    );

    const string k_ReleaseId = "00000000-0000-0000-0000-00000000rel2";
    const string k_ReleaseName = "Release-2";
    const string k_ReleaseDescription = "Description-2";

    static readonly ReleaseResponse k_ReleaseResponse = new(
        k_ReleaseId,
        k_ReleaseName,
        k_ReleaseDescription,
        k_ValidEnvironmentId
    );

    // Valid Release Pointers
    static readonly PointerResponse k_PointerResponseWithRef = new(
        k_PointerWithRefId,
        k_PointerWithRefName,
        k_PointerWithRefDescription,
        k_ValidEnvironmentId,
        release: k_ReleaseResponseWithPointer
    );

    const string k_PointerId = "00000000-0000-0000-0000-00000000ptr2";
    const string k_PointerName = "beta";
    const string k_PointerDescription = "beta pointer";

    static readonly PointerResponse k_PointerResponse = new(
        k_PointerId,
        k_PointerName,
        k_PointerDescription,
        k_ValidEnvironmentId
    );

    // Valid Targeting Rules
    static readonly TargetingRule k_TargetingRule1 = new(
        name: "rule_1",
        description: "Description-1",
        condition: "unity.platform == macos",
        variants: [],
        enabled: true,
        priority: 1000,
        rolloutPercentage: 100,
        startDate: default,
        endDate: default
        );

    static readonly TargetingRule k_TargetingRule2 = new(
        name: "rule_2",
        description: "Description-2",
        condition: "unity.platform == macos",
        variants: [],
        enabled: true,
        priority: 1000,
        rolloutPercentage: 100,
        startDate: default,
        endDate: default
        );

    // ~~~~~~~~~ INIT ~~~~~~~~~
    public async Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        var liveContentServiceModels = await MappingModelUtils.ParseMappingModelsFromGeneratorConfigAsync(
            "live-releases-v1-generator-config.yaml",
            new WireMockOpenApiParserSettings());
        return liveContentServiceModels.ToArray();
    }

    public void CustomMock(WireMockServer mockServer)
    {
        var responseHeaders = new Dictionary<string, WireMockList<string>>
        {
            { "Content-Type", new WireMockList<string>("application/json") },
            {
                "unity-ratelimit",
                new WireMockList<string>("limit=40,remaining=39,reset=1;limit=100000,remaining=99999,reset=1800")
            }
        };

        MockGetReleases(mockServer, responseHeaders);
        MockGetRelease(mockServer, responseHeaders);
        MockCreateRelease(mockServer, responseHeaders);
        MockUpdateRelease(mockServer, responseHeaders);
        MockDeletePointer(mockServer, responseHeaders);

        MockGetPointers(mockServer, responseHeaders);
        MockGetPointer(mockServer, responseHeaders);
        MockCreatePointer(mockServer, responseHeaders);
        MockUpdatePointer(mockServer, responseHeaders);

        MockGetTargetingRules(mockServer, responseHeaders);
        MockGetTargetingRule(mockServer, responseHeaders);
        MockDeleteTargetingRule(mockServer, responseHeaders);
        MockCreateTargetingRule(mockServer, responseHeaders);
        MockUpdateTargetingRule(mockServer, responseHeaders);
    }

    static void MockGetReleases(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        responseHeaders["Content-Range"] = "items 0-1/2";
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/releases"
                    )
                    .WithParam("limit", "20")
                    .WithParam("page", "1")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new List<ReleaseResponse>
                        {
                            k_ReleaseResponse,
                            k_ReleaseResponseWithPointer
                        }
                    )
                    .WithStatusCode(200));
    }

    static void MockGetPointers(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        responseHeaders["Content-Range"] = "items 0-1/2";
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/releasepointers"
                    )
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new List<PointerResponse>
                        {
                            k_PointerResponse,
                            k_PointerResponseWithRef
                        }
                    )
                    .WithStatusCode(200));
    }


    static void MockCreateRelease(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/releases"
                    )
                    .UsingPost())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_ReleaseResponse)
                    .WithStatusCode(201));
    }

    static void MockUpdatePointer(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/releasepointers/live"
                    )
                    .WithBody(
                        new JsonPartialMatcher(
                            """
                            {
                                "description": "live pointer",
                                "releaseName": "Release-2",
                            }
                            """
                        ))
                    .UsingPatch())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_PointerResponseWithRef)
                    .WithStatusCode(201));
    }

    static void MockCreatePointer(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/releasepointers"
                    )
                    .WithBody(
                        new JsonPartialMatcher(
                            """
                            {
                                "name": "beta",
                                "description": "beta pointer",
                                "releaseName": "Release-1"
                            }
                            """
                        ))
                    .UsingPost())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_PointerResponse)
                    .WithStatusCode(201));
    }

    static void MockGetPointer(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        var pointers = new Dictionary<string, PointerResponse>
        {
            { "live", k_PointerResponseWithRef },
            { "beta", k_PointerResponse }
        };

        foreach (var pointer in pointers)
        {
            mockServer
                .Given(
                    Request.Create()
                        .WithPath(
                            $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/releasepointers/{pointer.Key}"
                        )
                        .UsingGet())
                .RespondWith(
                    Response.Create()
                        .WithHeaders(responseHeaders)
                        .WithBodyAsJson(pointer.Value)
                        .WithStatusCode(201));
        }
    }

    static void MockDeletePointer(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/releasepointers/live"
                    )
                    .UsingDelete())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithStatusCode(201));
    }

    static void MockUpdateRelease(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/releases/Release-1"
                    )
                    .WithBody(
                        new JsonPartialMatcher(
                            """
                            {
                                "description": "Description1",
                            }
                            """
                        ))
                    .UsingPatch())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_ReleaseResponse)
                    .WithStatusCode(201));
    }

    static void MockGetRelease(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        var releases = new Dictionary<string, ReleaseResponse>
        {
            { "Release-1", k_ReleaseResponseWithPointer },
            { "Release-2", k_ReleaseResponse }
        };

        foreach (var release in releases)
            mockServer
                .Given(
                    Request.Create()
                        .WithPath(
                            $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/releases/{release.Key}"
                        )
                        .UsingGet())
                .RespondWith(
                    Response.Create()
                        .WithHeaders(responseHeaders)
                        .WithBodyAsJson(release.Value)
                        .WithStatusCode(201));
    }

    static void MockGetTargetingRules(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/targeting/rules"
                    )
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(new List<TargetingRule>() { k_TargetingRule1 })
                    .WithStatusCode(200));
    }

    static void MockGetTargetingRule(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        foreach (var rule in new List<TargetingRule>() { k_TargetingRule1, k_TargetingRule2 })
        {
            mockServer
                .Given(
                    Request.Create()
                        .WithPath(
                            $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/targeting/rules/{rule.Name}"
                        )
                        .UsingGet())
                .RespondWith(
                    Response.Create()
                        .WithHeaders(responseHeaders)
                        .WithBodyAsJson(rule)
                        .WithStatusCode(200));
        }
    }

    static void MockDeleteTargetingRule(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        foreach (var rule in new List<TargetingRule>() { k_TargetingRule1, k_TargetingRule2 })
        {
            mockServer
                .Given(
                    Request.Create()
                        .WithPath(
                            $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/targeting/rules/{rule.Name}"
                        )
                        .UsingDelete())
                .RespondWith(
                    Response.Create()
                        .WithHeaders(responseHeaders)
                        .WithStatusCode(204));
        }
    }

    static void MockCreateTargetingRule(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        var bodyJson1 = """
                        {
                            "name": "rule_1",
                            "condition": "user.name == player-1234",
                            "variants": [{"name": "grey", "weight": 90, "tags": ["cat"]}],
                            "rolloutPercentage": 30
                        }
                        """;
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/targeting/rules"
                    )
                    .WithBody(new JsonPartialMatcher(bodyJson1))
                    .UsingPost())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_TargetingRule1)
                    .WithStatusCode(200));

        var bodyJson2 = $"{{ \"name\": \"{k_TargetingRule2.Name}\"}}";
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/targeting/rules"
                    )
                    .WithBody(new JsonPartialMatcher(bodyJson2))
                    .UsingPost())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithStatusCode(409));
    }

    static void MockUpdateTargetingRule(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        foreach (var rule in new List<TargetingRule>() { k_TargetingRule1, k_TargetingRule2 })
        {
            mockServer
                .Given(
                    Request.Create()
                        .WithPath(
                            $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/targeting/rules/{rule.Name}"
                        )
                        .UsingPut())
                .RespondWith(
                    Response.Create()
                        .WithHeaders(responseHeaders)
                        .WithStatusCode(200));
        }
    }
}
