using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Gateway.LiveContentApiV1.Generated.Model;
using WireMock.Admin.Mappings;
using WireMock.Matchers;
using WireMock.Net.OpenApiParser.Settings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;

namespace Unity.Services.Cli.MockServer.ServiceMocks;

public class LiveContentApiMock : IServiceApiMock
{
    const string k_BasePath = "/v1";

    static readonly LcmGetAssets200ResponseInner k_UploadedConfigSample = new()
    {
        Id = "00000-0000-0000-0000-000000000000",
        Path = "configs/config.json",
        Type = LcmGetAssets200ResponseInner.TypeEnum.Config,
        ContentUri = "https://cdn.example.com/file/id",
        ContentType = "application/json",
        ContentHash = "456fjakj=z/zveds878",
        ContentSize = 100,
        VariantTag = ["gameplay"],
        CreatedAt = DateTime.MinValue,
        UpdatedAt = DateTime.MinValue,
        Complete = true,
        Metadata = new Dictionary<string, object>
        {
            ["key"] = "value",
            ["deployedBy"] = "FILES.lcf"
        }
    };

    static readonly LcmGetAssets200ResponseInner k_UploadedConfigSampleAtl = new()
    {
        Id = "00000-0000-0000-0000-000000000000",
        Path = "player.json",
        Type = LcmGetAssets200ResponseInner.TypeEnum.Config,
        ContentUri = "https://cdn.example.com/player.json",
        ContentType = "application/json",
        ContentHash = "000fjakj=z/zveds000",
        ContentSize = 200,
        VariantTag = ["gameplay"],
        CreatedAt = DateTime.MinValue,
        UpdatedAt = DateTime.MinValue,
        Complete = true,
        Metadata = new Dictionary<string, object>
        {
            ["key"] = "value",
            ["deployedBy"] = "FILES.lcf"
        },
        Schemas = ["https://schema.unity.com/example"]
    };

    static readonly LcmGetAssets200ResponseInner k_UploadedFileSample = new()
    {
        Id = "00000-0000-0000-0000-000000000001",
        Path = "assets/background.ext",
        Type = LcmGetAssets200ResponseInner.TypeEnum.File,
        ContentUri = "invalidUrl",
        ContentType = "application/json",
        ContentHash = "456fjakj=z/zveds879",
        ContentSize = 100,
        VariantTag = ["assets"],
        CreatedAt = DateTime.MinValue,
        UpdatedAt = DateTime.MinValue,
        Complete = true,
        Metadata = new Dictionary<string, object>
        {
            ["type"] = "background",
            ["deployedBy"] = "FILES.lcf"
        }
    };

    static readonly LcmGetAssets200ResponseInner k_NotUploadedFileSample = new()
    {
        Id = "00000-0000-0000-0000-000000000002",
        Path = "assets/file.ext",
        Type = LcmGetAssets200ResponseInner.TypeEnum.File,
        ContentUri = "https://cdn.example.com/file/id",
        ContentType = "application/json",
        ContentHash = "456fjakj=z/zveds879",
        ContentSize = 100,
        VariantTag = ["assets"],
        CreatedAt = DateTime.MinValue,
        UpdatedAt = DateTime.MinValue,
        Complete = false,
        Metadata = new Dictionary<string, object>()
        {
            ["type"] = "background"
        }
    };

    static LcmGetAssets200ResponseInner CreateAuthoringFileSample0(string mockServerUrl)
    {
        return new LcmGetAssets200ResponseInner
        {
            Id = "00000-0000-0000-0000-000000000000",
            Path = "file.0",
            Type = LcmGetAssets200ResponseInner.TypeEnum.File,
            ContentUri = $"{mockServerUrl}/content/example",
            ContentType = "strean/octet-stream",
            ContentHash = "056fjakj=z/zveds879",
            ContentSize = 10,
            VariantTag = ["ios"],
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue,
            Complete = true,
            Metadata = new Dictionary<string, object>()
            {
                ["deployedBy"] = "FILES.lcf"
            }
        };
    }

    static LcmGetAssets200ResponseInner CreateAuthoringFileSample1(string mockServerUrl)
    {
        return new LcmGetAssets200ResponseInner
        {
            Id = "00000-0000-0000-0000-000000000001",
            Path = "file.1",
            Type = LcmGetAssets200ResponseInner.TypeEnum.File,
            ContentUri = $"{mockServerUrl}/content/example",
            ContentType = "strean/octet-stream",
            ContentHash = "156fjakj=z/zveds879",
            ContentSize = 11,
            VariantTag = ["ios"],
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue,
            Complete = true,
            Metadata = new Dictionary<string, object>()
            {
                ["deployedBy"] = "FILES.lcf"
            }
        };
    }

    static LcmGetAssets200ResponseInner CreateAuthoringFileSample2(string mockServerUrl)
    {
        return new LcmGetAssets200ResponseInner
        {
            Id = "00000-0000-0000-0000-000000000002",
            Path = "file.2",
            Type = LcmGetAssets200ResponseInner.TypeEnum.File,
            ContentUri = $"{mockServerUrl}/content/example",
            ContentType = "application/octet-stream",
            ContentHash = "lZfYmIFPFlt+1hGHIsJCcf7IwSVNRuQ3rWqyQFB2Pi0=",
            ContentSize = 10,
            VariantTag = ["ios"],
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue,
            Complete = true,
            Metadata = new Dictionary<string, object>()
            {
                ["deployedBy"] = "FILES.lcf"
            }
        };
    }

    static LcmGetAssets200ResponseInner CreateAuthoringFileSample3(string mockServerUrl)
    {
        return new LcmGetAssets200ResponseInner
        {
            Id = "00000-0000-0000-0000-000000000002",
            Path = "file.3",
            Type = LcmGetAssets200ResponseInner.TypeEnum.File,
            ContentUri = $"{mockServerUrl}/content/example",
            ContentType = "application/octet-stream",
            ContentHash = "lZfYmIFPFlt+1hGHIsJCcf7IwSVNRuQ3rWqyQFB2Pi0=",
            ContentSize = 10,
            VariantTag = ["ios"],
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue,
            Complete = false,
            SignedUrl = $"{mockServerUrl}/upload/example",
            Metadata = new Dictionary<string, object>()
            {
                ["deployedBy"] = "FILES.lcf"
            }
        };
    }

    static LcmGetAssets200ResponseInner CreateAuthoringConfigSample0(string mockServerUrl)
    {
        return new LcmGetAssets200ResponseInner
        {
            Id = "00000-0000-0000-0000-000000000000",
            Path = "config.0",
            Type = LcmGetAssets200ResponseInner.TypeEnum.Config,
            ContentUri = $"{mockServerUrl}/content/example",
            ContentType = "application/json",
            ContentHash = "056fjakj=z/zveds879",
            ContentSize = 10,
            VariantTag = ["ios"],
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue,
            Complete = true,
            Metadata = new Dictionary<string, object>()
        };
    }

    static LcmGetAssets200ResponseInner CreateAuthoringConfigSample1(string mockServerUrl)
    {
        return new LcmGetAssets200ResponseInner
        {
            Id = "00000-0000-0000-0000-000000000001",
            Path = "config.1",
            Type = LcmGetAssets200ResponseInner.TypeEnum.Config,
            ContentUri = $"{mockServerUrl}/content/example",
            ContentType = "application/json",
            ContentHash = "aDk+03NSSYDPprDBxbLQuA==",
            ContentSize = 14,
            VariantTag = ["ios"],
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue,
            Complete = true,
            Metadata = new Dictionary<string, object>()
        };
    }

    static LcmGetAssets200ResponseInner CreateAuthoringConfigSample2(string mockServerUrl)
    {
        return new LcmGetAssets200ResponseInner
        {
            Id = "00000-0000-0000-0000-000000000001",
            Path = "config.2",
            Type = LcmGetAssets200ResponseInner.TypeEnum.Config,
            ContentUri = $"{mockServerUrl}/content/example",
            ContentType = "application/json",
            ContentHash = "Mb2dejoHkpqptLY50Uf5hg==",
            ContentSize = 14,
            VariantTag = ["ios"],
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue,
            Complete = true,
            Metadata = new Dictionary<string, object>()
        };
    }

    static LcmGetAssets200ResponseInner CreateAuthoringConfigSample3(string mockServerUrl)
    {
        return new LcmGetAssets200ResponseInner
        {
            Id = "00000-0000-0000-0000-000000000001",
            Path = "config.3",
            Type = LcmGetAssets200ResponseInner.TypeEnum.Config,
            ContentUri = $"{mockServerUrl}/content/example",
            ContentType = "application/json",
            ContentHash = "sB6WFUDVrrwLPKUWUfE/cA==",
            ContentSize = 14,
            VariantTag = ["ios"],
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue,
            Complete = true,
            Metadata = new Dictionary<string, object>()
        };
    }

    static LcmGetAssets200ResponseInner CreateAuthoringConfigSampleUpdate(string mockServerUrl)
    {
        return new LcmGetAssets200ResponseInner
        {
            Id = "00000-0000-0000-0000-000000000000",
            Path = "updated.lcc",
            Type = LcmGetAssets200ResponseInner.TypeEnum.Config,
            ContentUri = $"{mockServerUrl}/content/example",
            ContentType = "application/json",
            ContentHash = "056fjakj=z/zveds879",
            ContentSize = 10,
            VariantTag = [],
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue,
            Complete = true,
            Metadata = new Dictionary<string, object>()
        };
    }

    static LcmGetAssets200ResponseInner CreateAuthoringConfigSampleNewRemote(string mockServerUrl)
    {
        return new LcmGetAssets200ResponseInner
        {
            Id = "00000-0000-0000-0000-000000000001",
            Path = "new-remote.lcc",
            Type = LcmGetAssets200ResponseInner.TypeEnum.Config,
            ContentUri = $"{mockServerUrl}/content/example",
            ContentType = "application/json",
            ContentHash = "aDk+03NSSYDPprDBxbLQuA==",
            ContentSize = 14,
            VariantTag = [],
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue,
            Complete = true,
            Metadata = new Dictionary<string, object>()
        };
    }

    static LcmGetAssets200ResponseInner CreateAuthoringConfigSampleNewLocal(string mockServerUrl)
    {
        return new LcmGetAssets200ResponseInner
        {
            Id = "00000-0000-0000-0000-000000000001",
            Path = "new-local.lcc",
            Type = LcmGetAssets200ResponseInner.TypeEnum.Config,
            ContentUri = $"{mockServerUrl}/content/example",
            ContentType = "application/json",
            ContentHash = "aDk+03NSSYDPprDBxbLQuA==",
            ContentSize = 14,
            VariantTag = [],
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue,
            Complete = true,
            Metadata = new Dictionary<string, object>()
        };
    }

    static LcmGetAssets200ResponseInner CreateAuthoringConfigVariantSampleUpdate(string mockServerUrl)
    {
        return new LcmGetAssets200ResponseInner
        {
            Id = "00000-0000-0000-0000-000000000000",
            Path = "subfolder/config2.lcc",
            Type = LcmGetAssets200ResponseInner.TypeEnum.Config,
            ContentUri = $"{mockServerUrl}/content/example",
            ContentType = "application/json",
            ContentHash = "056fjakj=z/zveds879",
            ContentSize = 10,
            VariantTag = ["VARIANT1"],
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue,
            Complete = true,
            Metadata = new Dictionary<string, object>()
        };
    }

    static LcmGetAssets200ResponseInner CreateAuthoringConfigVariantSampleNewRemote(string mockServerUrl)
    {
        return new LcmGetAssets200ResponseInner
        {
            Id = "00000-0000-0000-0000-000000000001",
            Path = "config2.lcc",
            Type = LcmGetAssets200ResponseInner.TypeEnum.Config,
            ContentUri = $"{mockServerUrl}/content/example",
            ContentType = "application/json",
            ContentHash = "aDk+03NSSYDPprDBxbLQuA==",
            ContentSize = 14,
            VariantTag = ["VARIANT1"],
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue,
            Complete = true,
            Metadata = new Dictionary<string, object>()
        };
    }

    static LcmGetAssets200ResponseInner CreateAuthoringConfigVariantSampleNewLocal(string mockServerUrl)
    {
        return new LcmGetAssets200ResponseInner
        {
            Id = "00000-0000-0000-0000-000000000001",
            Path = "config1.lcc",
            Type = LcmGetAssets200ResponseInner.TypeEnum.Config,
            ContentUri = $"{mockServerUrl}/content/example",
            ContentType = "application/json",
            ContentHash = "aDk+03NSSYDPprDBxbLQuA==",
            ContentSize = 14,
            VariantTag = ["VARIANT1"],
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue,
            Complete = true,
            Metadata = new Dictionary<string, object>()
        };
    }

    // ~~~~~~~~~ INIT ~~~~~~~~~

    public async Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        var liveContentServiceModels = await MappingModelUtils.ParseMappingModelsFromGeneratorConfigAsync(
            "livecontent-v1-generator-config.yaml",
            new WireMockOpenApiParserSettings());
        return liveContentServiceModels.ToArray();
    }

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
                        new List<LcmGetAssets200ResponseInner>
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
                        new List<LcmGetAssets200ResponseInner>
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
        var fileSample = CreateAuthoringConfigSample0(mockServer.Url!);
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
                        new List<LcmGetAssets200ResponseInner>
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
                            updated = new List<LcmGetAssets200ResponseInner>
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
                            created = new List<LcmGetAssets200ResponseInner>
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
        var authoringConfigSample0 = CreateAuthoringConfigSample0(mockServer.Url!);
        var authoringConfigSample1 = CreateAuthoringConfigSample1(mockServer.Url!);
        var authoringConfigSample2 = CreateAuthoringConfigSample2(mockServer.Url!);
        var authoringConfigSample3 = CreateAuthoringConfigSample3(mockServer.Url!);

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
                        new List<LcmGetAssets200ResponseInner>
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
                            updated = new List<LcmGetAssets200ResponseInner>
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
                            created = new List<LcmGetAssets200ResponseInner>
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
        var authoringConfigSampleUpdated = CreateAuthoringConfigSampleUpdate(mockServer.Url!);
        var authoringConfigSampleNewRemote = CreateAuthoringConfigSampleNewRemote(mockServer.Url!);
        var authoringConfigSampleNewLocal = CreateAuthoringConfigSampleNewLocal(mockServer.Url!);

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
                        new List<LcmGetAssets200ResponseInner>
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
                        new List<LcmGetAssets200ResponseInner>
                        {
                            authoringConfigSampleUpdated,
                        })
                    .WithStatusCode(200));
    }

    static void MockConfigVariantList(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        var authoringConfigVariantSampleUpdated = CreateAuthoringConfigVariantSampleUpdate(mockServer.Url!);
        var authoringConfigVariantSampleNewRemote = CreateAuthoringConfigVariantSampleNewRemote(mockServer.Url!);
        var authoringConfigVariantSampleNewLocal = CreateAuthoringConfigVariantSampleNewLocal(mockServer.Url!);

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
                        new List<LcmGetAssets200ResponseInner>
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
        var authoringConfigSampleUpdated = CreateAuthoringConfigSampleUpdate(mockServer.Url!);
        var authoringConfigSampleNewRemote = CreateAuthoringConfigSampleNewRemote(mockServer.Url!);
        var authoringConfigSampleNewLocal = CreateAuthoringConfigSampleNewLocal(mockServer.Url!);

        var authoringConfigVariantSampleUpdated = CreateAuthoringConfigVariantSampleUpdate(mockServer.Url!);
        var authoringConfigVariantSampleNewRemote = CreateAuthoringConfigVariantSampleNewRemote(mockServer.Url!);
        var authoringConfigVariantSampleNewLocal = CreateAuthoringConfigVariantSampleNewLocal(mockServer.Url!);

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
                            updated = new List<LcmGetAssets200ResponseInner>
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
                            created = new List<LcmGetAssets200ResponseInner>
                            {
                                authoringConfigVariantSampleNewLocal,
                                authoringConfigSampleNewLocal
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
