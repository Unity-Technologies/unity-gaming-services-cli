using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Gateway.LiveContentApiV1.Generated.Model;
using WireMock.Admin.Mappings;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;

namespace Unity.Services.Cli.MockServer.ServiceMocks;

public class LiveContentApiMock : IServiceApiMock
{
    const string k_BasePath = "/v1";

    static readonly LcmGetAssets200ResponseInnerOneOf1 k_UploadedConfigSample = CreateConfigSample(
        "00000-0000-0000-0000-000000000000", "configs/config.json", "456fjakj=z/zveds878", 100,
        ["gameplay"], new Dictionary<string, object> { ["key"] = "value", ["deployedBy"] = "FILES.lcf" });

    static readonly LcmGetAssets200ResponseInnerOneOf1 k_UploadedConfigSampleAtl = CreateConfigSample(
        "00000-0000-0000-0000-000000000000", "player.json", "000fjakj=z/zveds000", 200,
        ["gameplay"], new Dictionary<string, object> { ["key"] = "value", ["deployedBy"] = "FILES.lcf" },
        ["https://schema.unity.com/example"]);

    static readonly LcmGetAssets200ResponseInnerOneOf k_UploadedFileSample = CreateFileSample(
        "00000-0000-0000-0000-000000000001", "assets/background.ext", "invalidUrl", "application/json",
        "456fjakj=z/zveds879", 100, ["assets"], true,
        new Dictionary<string, object> { ["type"] = "background", ["deployedBy"] = "FILES.lcf" });

    static readonly LcmGetAssets200ResponseInnerOneOf k_NotUploadedFileSample = CreateFileSample(
        "00000-0000-0000-0000-000000000002", "assets/file.ext", "https://cdn.example.com/file/id",
        "application/json", "456fjakj=z/zveds879", 100, ["assets"], false,
        new Dictionary<string, object> { ["type"] = "background" });

    static LcmGetAssets200ResponseInnerOneOf CreateFileSample(
        string id,
        string path,
        string contentUri,
        string contentType,
        string contentHash,
        long contentSize,
        List<string> variantTag,
        bool complete,
        Dictionary<string, object> metadata,
        string? signedUrl = null) => new(
            contentType: contentType,
            id: id,
            path: path,
            type: LcmGetAssets200ResponseInnerOneOf.TypeEnum.File,
            variants:
            [
                new LcmGetAssets200ResponseInnerOneOfVariantsInner(
                    complete: complete,
                    contentHash: contentHash,
                    contentSize: contentSize,
                    contentUri: contentUri,
                    createdAt: DateTime.MinValue,
                    metadata: metadata,
                    signedUrl: signedUrl!,
                    updatedAt: DateTime.MinValue,
                    variantTag: variantTag)
            ]);

    static LcmGetAssets200ResponseInnerOneOf1 CreateConfigSample(
        string id,
        string path,
        string contentHash,
        long contentSize,
        List<string> variantTag,
        Dictionary<string, object> metadata,
        List<string>? schemas = null) => new(
            contentType: "application/json",
            id: id,
            path: path,
            schemas: schemas!,
            type: LcmGetAssets200ResponseInnerOneOf1.TypeEnum.Config,
            variants:
            [
                new LcmGetAssets200ResponseInnerOneOf1VariantsInner(
                    complete: true,
                    contentHash: contentHash,
                    contentSize: contentSize,
                    createdAt: DateTime.MinValue,
                    metadata: metadata,
                    updatedAt: DateTime.MinValue,
                    variantTag: variantTag)
            ]);

    static LcmGetAssets200ResponseInnerOneOf CreateAuthoringFileSample0(string mockServerUrl) => CreateFileSample(
        "00000-0000-0000-0000-000000000000", "file.0", $"{mockServerUrl}/content/example",
        "strean/octet-stream", "056fjakj=z/zveds879", 10, ["ios"], true,
        new Dictionary<string, object> { ["deployedBy"] = "FILES.lcf" });

    static LcmGetAssets200ResponseInnerOneOf CreateAuthoringFileSample1(string mockServerUrl) => CreateFileSample(
        "00000-0000-0000-0000-000000000001", "file.1", $"{mockServerUrl}/content/example",
        "strean/octet-stream", "156fjakj=z/zveds879", 11, ["ios"], true,
        new Dictionary<string, object> { ["deployedBy"] = "FILES.lcf" });

    static LcmGetAssets200ResponseInnerOneOf CreateAuthoringFileSample2(string mockServerUrl) => CreateFileSample(
        "00000-0000-0000-0000-000000000002", "file.2", $"{mockServerUrl}/content/example",
        "application/octet-stream", "lZfYmIFPFlt+1hGHIsJCcf7IwSVNRuQ3rWqyQFB2Pi0=", 10, ["ios"], true,
        new Dictionary<string, object> { ["deployedBy"] = "FILES.lcf" });

    static LcmGetAssets200ResponseInnerOneOf CreateAuthoringFileSample3(string mockServerUrl) => CreateFileSample(
        "00000-0000-0000-0000-000000000002", "file.3", $"{mockServerUrl}/content/example",
        "application/octet-stream", "lZfYmIFPFlt+1hGHIsJCcf7IwSVNRuQ3rWqyQFB2Pi0=", 10, ["ios"], false,
        new Dictionary<string, object> { ["deployedBy"] = "FILES.lcf" }, $"{mockServerUrl}/upload/example");

    static LcmGetAssets200ResponseInnerOneOf1 CreateAuthoringConfigSample0() => CreateConfigSample(
        "00000-0000-0000-0000-000000000000", "config.0", "056fjakj=z/zveds879", 10, ["ios"], []);

    static LcmGetAssets200ResponseInnerOneOf1 CreateAuthoringConfigSample1() => CreateConfigSample(
        "00000-0000-0000-0000-000000000001", "config.1", "aDk+03NSSYDPprDBxbLQuA==", 14, ["ios"], []);

    static LcmGetAssets200ResponseInnerOneOf1 CreateAuthoringConfigSample2() => CreateConfigSample(
        "00000-0000-0000-0000-000000000001", "config.2", "Mb2dejoHkpqptLY50Uf5hg==", 14, ["ios"], []);

    static LcmGetAssets200ResponseInnerOneOf1 CreateAuthoringConfigSample3() => CreateConfigSample(
        "00000-0000-0000-0000-000000000001", "config.3", "sB6WFUDVrrwLPKUWUfE/cA==", 14, ["ios"], []);

    static LcmGetAssets200ResponseInnerOneOf1 CreateAuthoringConfigSampleUpdate() => CreateConfigSample(
        "00000-0000-0000-0000-000000000000", "updated.lcc", "056fjakj=z/zveds879", 10, [], []);

    static LcmGetAssets200ResponseInnerOneOf1 CreateAuthoringConfigSampleNewRemote() => CreateConfigSample(
        "00000-0000-0000-0000-000000000001", "new-remote.lcc", "aDk+03NSSYDPprDBxbLQuA==", 14, [], []);

    static LcmGetAssets200ResponseInnerOneOf1 CreateAuthoringConfigSampleNewLocal() => CreateConfigSample(
        "00000-0000-0000-0000-000000000001", "new-local.lcc", "aDk+03NSSYDPprDBxbLQuA==", 14, [], []);

    static LcmGetAssets200ResponseInnerOneOf1 CreateAuthoringConfigVariantSampleUpdate() => CreateConfigSample(
        "00000-0000-0000-0000-000000000000", "subfolder/config2.lcc", "056fjakj=z/zveds879", 10,
        ["VARIANT1"], []);

    static LcmGetAssets200ResponseInnerOneOf1 CreateAuthoringConfigVariantSampleNewRemote() => CreateConfigSample(
        "00000-0000-0000-0000-000000000001", "config2.lcc", "aDk+03NSSYDPprDBxbLQuA==", 14,
        ["VARIANT1"], []);

    static LcmGetAssets200ResponseInnerOneOf1 CreateAuthoringConfigVariantSampleNewLocal() => CreateConfigSample(
        "00000-0000-0000-0000-000000000001", "config1.lcc", "aDk+03NSSYDPprDBxbLQuA==", 14,
        ["VARIANT1"], []);

    // ~~~~~~~~~ INIT ~~~~~~~~~

    public Task<IReadOnlyList<MappingModel>> CreateMappingModels() =>
        Task.FromResult<IReadOnlyList<MappingModel>>([]);

    public void CustomMock(WireMockServer mockServer)
    {
        var responseHeaders = new Dictionary<string, WireMockList<string>>
        {
            { "Content-Type", new WireMockList<string>("application/json") },
            { "unity-ratelimit", new WireMockList<string>("limit=40,remaining=39,reset=1;limit=100000,remaining=99999,reset=1800") }
        };

        MockFilesGet(mockServer, responseHeaders);
        MockFileGet(mockServer, responseHeaders);
        MockFileCreate(mockServer, responseHeaders);
        MockFileUpdate(mockServer, responseHeaders);
        MockFileDelete(mockServer, responseHeaders);
        MockFileDeploy(mockServer, responseHeaders);

        MockConfigsGet(mockServer, responseHeaders);
        MockConfigGet(mockServer, responseHeaders);
        MockConfigGetContent(mockServer, responseHeaders);
        MockConfigCreate(mockServer, responseHeaders);
        MockConfigUpdate(mockServer, responseHeaders);
        MockConfigDelete(mockServer, responseHeaders);
        MockConfigDeployOldDeleteMe(mockServer, responseHeaders);

        MockConfigList(mockServer, responseHeaders);
        MockConfigVariantList(mockServer, responseHeaders);
        MockConfigUpdateDeploy(mockServer, responseHeaders);

        MockContentDownloads(mockServer);
        MockS3Upload(mockServer);
    }

    static void MockFilesGet(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        responseHeaders["Content-Range"] = "items 0-1/4";
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/files/info"
                    )
                    .WithParam("limit", "2")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new List<LcmGetAssets200ResponseInnerOneOf>
                        {
                            k_UploadedFileSample,
                            k_NotUploadedFileSample
                        })
                    .WithStatusCode(200));
    }

    static void MockFileGet(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        // Normal get file
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/files/info/assets/background.ext")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_UploadedFileSample)
                    .WithStatusCode(200));

        // Get file with dynamic content URL for mock download
        var fileSample = CreateAuthoringFileSample0(mockServer.Url!);
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/files/info/file.0")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(fileSample)
                    .WithStatusCode(200));
    }

    static void MockConfigCreate(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        const string basePath =
            $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/info";
        var baseResponse = Response.Create()
            .WithHeaders(responseHeaders)
            .WithBodyAsJson(k_UploadedConfigSample)
            .WithStatusCode(200);

        var configVariants = new[]
        {
            // With Schema in config
            """
            {
                "schema": "https://cdn.example.com/schema.json",
                "name": "test-live-content",
                "settings": {
                    "enabled": true,
                    "maxConnections": 100,
                    "timeout": 30
                },
                "$metadata": {}
            }
            """,

            // Without schema
            """
            {
                "name": "test-live-content",
                "$metadata": {}
            }
            """
        };

        foreach (var bodyJson in configVariants)
        {
            mockServer
                .Given(
                    Request.Create()
                        .WithPath($"{basePath}/input.json")
                        .WithParam("variantTag", "gameplay")
                        .WithBody(new JsonPartialMatcher(bodyJson))
                        .UsingPost())
                .RespondWith(baseResponse);
        }

        // 409 - already exist
        mockServer
            .Given(
                Request.Create()
                    .WithPath($"{basePath}/already.exist")
                    .WithParam("variantTag", "gameplay")
                    .UsingPost())
            .RespondWith(Response.Create().WithStatusCode(409));
    }

    static void MockConfigsGet(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        responseHeaders["Content-Range"] = "items 0-1/4";
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/info"
                    )
                    .WithParam("limit", "2")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new List<LcmGetAssets200ResponseInnerOneOf1>
                        {
                            k_UploadedConfigSample,
                            k_UploadedConfigSampleAtl
                        })
                    .WithStatusCode(200));
    }

    static void MockConfigGetContent(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/content/*")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new Dictionary<string, object>
                        {
                            ["name"] = "test-live-content",
                            ["settings"] = new Dictionary<string, object>
                            {
                                ["enabled"] = true,
                                ["maxConnections"] = 100,
                                ["timeout"] = 30
                            }
                        })
                    .WithStatusCode(200));
    }

    static void MockConfigGet(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        // Normal get file
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/info/player.json")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_UploadedConfigSampleAtl)
                    .WithStatusCode(200));

        // Get file with dynamic content URL for mock download
        var fileSample = CreateAuthoringConfigSample0();
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/info/config.0")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(fileSample)
                    .WithStatusCode(200));
    }

    static void MockConfigDelete(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/info/player.json"
                    )
                    .UsingDelete())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithStatusCode(204));
    }

    static void MockConfigUpdate(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/info/player.json"
                    )
                    .WithBody(
                        new JsonPartialMatcher(
                            """
                            {
                                "$metadata": {'type': 'background'}
                            }
                            """
                        ))
                    .UsingPatch())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_UploadedConfigSampleAtl)
                    .WithStatusCode(200));
    }

    static void MockFileCreate(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        var validFileSample = CreateAuthoringFileSample0(mockServer.Url!);
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/files/info/file.0"
                    )
                    .WithBody(
                        new JsonPartialMatcher(
                            """
                            {
                                "contentType": "application/octet-stream",
                                "contentSize": 12,
                                "contentHash": "4Kw2AQBd+hhk9Tkqq699iYsbW6uFTxrLRJG82Aa3aww=",
                            }
                            """
                        ))
                    .UsingPost())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(validFileSample)
                    .WithStatusCode(200));
    }

    static void MockFileDelete(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/files/info/assets/background.ext"
                    )
                    .UsingDelete())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithStatusCode(204));
    }

    static void MockFileUpdate(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/files/info/assets/background.ext"
                    )
                    .WithBody(
                        new JsonPartialMatcher(
                            """
                            {
                                "contentType": "image",
                                "metadata": { "test": 2 }
                            }
                            """
                        ))
                    .UsingPatch())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_UploadedFileSample)
                    .WithStatusCode(200));
    }

    static void MockFileDeploy(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        var authoringFileSample0 = CreateAuthoringFileSample0(mockServer.Url!);
        var authoringFileSample1 = CreateAuthoringFileSample1(mockServer.Url!);
        var authoringFileSample2 = CreateAuthoringFileSample2(mockServer.Url!);
        var authoringFileSample3 = CreateAuthoringFileSample3(mockServer.Url!);

        // Mock the List
        responseHeaders["Content-Range"] = "items 0-2/3";
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/files/info"
                    )
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new List<LcmGetAssets200ResponseInnerOneOf>
                        {
                            authoringFileSample0,
                            authoringFileSample1,
                            authoringFileSample2
                        })
                    .WithStatusCode(200));

        // Mock the Update Batch
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/files/batch/info"
                    )
                    .WithBody(
                        new JsonPartialMatcher(
                            """
                            [
                                {
                                    "path": "file.1",
                                    "contentSize": 9,
                                    "contentHash": "0ZiM0wGYJPB19hZ34ab1SxYDWGhIjkBRdX3eU63u+A8=",
                                    "contentType": "application/octet-stream",
                                    "variantTag": ["ios"],
                                }
                            ]
                            """
                        ))
                    .UsingPut())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new
                        {
                            updated = new List<LcmGetAssets200ResponseInnerOneOf>
                            {
                                authoringFileSample1
                            },
                            error = new List<object>()
                        })
                    .WithStatusCode(200));

        // Mock the Create Batch
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/files/batch/info"
                    )
                    .WithBody(
                        new JsonPartialMatcher(
                            """
                            [
                                {
                                    "path": "file.3",
                                    "contentSize": 9,
                                    "contentHash": "lZfYmIFPFlt+1hGHIsJCcf7IwSVNRuQ3rWqyQFB2Pi0=",
                                    "contentType": "application/octet-stream",
                                    "variantTag": ["ios"],
                                }
                            ]
                            """
                        ))
                    .UsingPost())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new
                        {
                            created = new List<LcmGetAssets200ResponseInnerOneOf>
                            {
                                authoringFileSample3
                            },
                            error = new List<object>()
                        })
                    .WithStatusCode(200));

        // Mock the Delete Batch
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/files/batch/info"
                    )
                    .WithBody(
                        new JsonPartialMatcher(
                            """
                            [
                                {
                                    "path": "file.0",
                                    "variantTag": ["ios"],
                                }
                            ]
                            """
                        ))
                    .UsingDelete())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new
                        {
                            error = new List<object>()
                        })
                    .WithStatusCode(200));
    }

    static void MockConfigDeployOldDeleteMe(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        var authoringConfigSample0 = CreateAuthoringConfigSample0();
        var authoringConfigSample1 = CreateAuthoringConfigSample1();
        var authoringConfigSample2 = CreateAuthoringConfigSample2();
        var authoringConfigSample3 = CreateAuthoringConfigSample3();

        // Mock the List
        responseHeaders["Content-Range"] = "items 0-2/3";
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/info"
                    )
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new List<LcmGetAssets200ResponseInnerOneOf1>
                        {
                            authoringConfigSample0,
                            authoringConfigSample1,
                            authoringConfigSample2
                        })
                    .WithStatusCode(200));

        // Mock the Update Batch
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/batch/info"
                    )
                    .WithBody(
                        new JsonPartialMatcher(
                            """
                            [
                                {
                                    "$path": "config.1",
                                    "$variantTag": ["ios"],
                                    "config": "1"
                                }
                            ]
                            """
                        ))
                    .UsingPut())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new
                        {
                            updated = new List<LcmGetAssets200ResponseInnerOneOf1>
                            {
                                authoringConfigSample1
                            },
                            error = new List<object>()
                        })
                    .WithStatusCode(200));

        // Mock the Create Batch
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/batch/info"
                    )
                    .WithBody(
                        new JsonPartialMatcher(
                            """
                            [
                                {
                                    "$path": "config.3",
                                    "$variantTag": ["ios"],
                                    "config": "3"
                                }
                            ]
                            """
                        ))
                    .UsingPost())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new
                        {
                            created = new List<LcmGetAssets200ResponseInnerOneOf1>
                            {
                                authoringConfigSample3
                            },
                            error = new List<object>()
                        })
                    .WithStatusCode(200));

        // Mock the Delete Batch
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/batch/info"
                    )
                    .WithBody(
                        new JsonPartialMatcher(
                            """
                            [
                                {
                                    "path": "config.0",
                                    "variantTag": ["ios"],
                                }
                            ]
                            """
                        ))
                    .UsingDelete())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new
                        {
                            error = new List<object>()
                        })
                    .WithStatusCode(200));
    }

    static void MockConfigList(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        var authoringConfigSampleUpdated = CreateAuthoringConfigSampleUpdate();
        var authoringConfigSampleNewRemote = CreateAuthoringConfigSampleNewRemote();
        var authoringConfigSampleNewLocal = CreateAuthoringConfigSampleNewLocal();

        // Mock the List
        responseHeaders["Content-Range"] = "items 0-2/3";
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/info"
                    )
                    .WithParam("noVariantTag", "true")
                    .WithParam("limit", "100")
                    .WithParam("start", "true")
                    .WithParam("path", ".lcc")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new List<LcmGetAssets200ResponseInnerOneOf1>
                        {
                            authoringConfigSampleUpdated,
                            authoringConfigSampleNewRemote
                        })
                    .WithStatusCode(200));

        // Mock the List single file
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/info"
                    )
                    .WithParam("noVariantTag", "true")
                    .WithParam("limit", "100")
                    .WithParam("start", "true")
                    .WithParam("path", "updated.lcc")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new List<LcmGetAssets200ResponseInnerOneOf1>
                        {
                            authoringConfigSampleUpdated,
                        })
                    .WithStatusCode(200));
    }

    static void MockConfigVariantList(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        var authoringConfigVariantSampleUpdated = CreateAuthoringConfigVariantSampleUpdate();
        var authoringConfigVariantSampleNewRemote = CreateAuthoringConfigVariantSampleNewRemote();
        var authoringConfigVariantSampleNewLocal = CreateAuthoringConfigVariantSampleNewLocal();

        // Mock the List
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/info"
                    )
                    .WithParam("variantTag", "variant1")
                    .WithParam("limit", "100")
                    .WithParam("start", "true")
                    .WithParam("path", ".lcc")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new List<LcmGetAssets200ResponseInnerOneOf1>
                        {
                            authoringConfigVariantSampleUpdated,
                            authoringConfigVariantSampleNewRemote
                        })
                    .WithStatusCode(200));

    }

    static void MockConfigUpdateDeploy(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        var authoringConfigSampleUpdated = CreateAuthoringConfigSampleUpdate();
        var authoringConfigSampleNewRemote = CreateAuthoringConfigSampleNewRemote();
        var authoringConfigSampleNewLocal = CreateAuthoringConfigSampleNewLocal();

        var authoringConfigVariantSampleUpdated = CreateAuthoringConfigVariantSampleUpdate();
        var authoringConfigVariantSampleNewRemote = CreateAuthoringConfigVariantSampleNewRemote();
        var authoringConfigVariantSampleNewLocal = CreateAuthoringConfigVariantSampleNewLocal();

        // Mock the Update Batch
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/batch/info"
                    )
                    .WithBody(
                        new JsonPartialMatcher(
                            """
                            [
                                {
                                    "$path": "updated.lcc",
                                },
                                {
                                    "$path": "subfolder/config2.lcc",
                                    "$variantTag": ["variant1"],
                                }
                            ]
                            """
                        ))
                    .UsingPut())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new
                        {
                            updated = new List<LcmGetAssets200ResponseInnerOneOf1>
                            {
                                authoringConfigVariantSampleUpdated,
                                authoringConfigSampleUpdated
                            },
                            error = new List<object>()
                        })
                    .WithStatusCode(200));

        // Mock the Create Batch
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/batch/info"
                    )
                    .WithBody(
                        new JsonPartialMatcher(
                            """
                            [
                                {
                                    "$path": "new-local.lcc",
                                },
                                {
                                    "$path": "config1.lcc",
                                    "$variantTag": ["variant1"],
                                }
                            ]
                            """
                        ))
                    .UsingPost())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new
                        {
                             created = new List<LcmGetAssets200ResponseInnerOneOf1>
                             {
                                 authoringConfigSampleNewLocal
                             },
                             error = new[]
                             {
                                 new LcmUpdateConfigs200ResponseErrorInner(
                                     error: "schema origin is not permitted",
                                     file: new LcmUpdateConfigs200ResponseErrorInnerFile(
                                         path: authoringConfigVariantSampleNewLocal.Path,
                                         variantTag: authoringConfigVariantSampleNewLocal.Variants[0].VariantTag))
                             }
                         })
                     .WithStatusCode(200));

        // Mock the Delete Batch
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/batch/info"
                    )
                    .WithBody(
                        new JsonPartialMatcher(
                            """
                            [
                                {
                                    "path": "new-remote.lcc"
                                },
                                {
                                    "path": "config2.lcc",
                                    "variantTag": ["VARIANT1"]
                                }
                            ]
                            """
                        ))
                    .UsingDelete())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(
                        new
                        {
                            error = new List<object>()
                        })
                    .WithStatusCode(200));
    }

    static void MockContentDownloads(WireMockServer mockServer)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath("/content/example")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithBody(
                        """
                        {
                            "name": "test-live-content",
                            "settings": {
                                "enabled": true,
                                "maxConnections": 100,
                                "timeout": 30
                            }
                        }
                        """)
                    .WithHeader("Content-Type", "application/json")
                    .WithStatusCode(200));
    }

    static void MockS3Upload(WireMockServer mockServer)
    {
        // Mock successful S3 upload
        mockServer
            .Given(
                Request.Create()
                    .WithPath("/upload/example")
                    .UsingPut())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200));

        // Mock S3 upload with error response for testing error handling
        mockServer
            .Given(
                Request.Create()
                    .WithPath("/upload/error")
                    .UsingPut())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(400)
                    .WithBody("Bad Request"));
    }
}
