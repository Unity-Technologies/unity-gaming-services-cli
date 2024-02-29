using System.Diagnostics;
using System.Net;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Gateway.CloudSaveApiV1.Generated.Model;
using WireMock.Admin.Mappings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Unity.Services.Cli.MockServer.ServiceMocks;

public class CloudSaveApiMock : IServiceApiMock
{
    const string k_CloudSavePath = "/cloud-save/v1";
    const string k_CloudSaveDataPath = "data";
    const string k_BaseUrl = $"{k_CloudSavePath}/{k_CloudSaveDataPath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}";

     static readonly GetIndexIdsResponse k_GetIndexIdsResponse = new GetIndexIdsResponse(
        new List<LiveIndexConfigInner>
        {
            new LiveIndexConfigInner(
                "testIndex1",
                LiveIndexConfigInner.EntityTypeEnum.Player,
                AccessClass.Default,
                IndexStatus.READY,
                new List<IndexField>()
                {
                    new IndexField("testIndexKey1", true)
                }
            ),
            new LiveIndexConfigInner(
                "testIndex2",
                LiveIndexConfigInner.EntityTypeEnum.Custom,
                AccessClass.Private,
                IndexStatus.BUILDING,
                new List<IndexField>()
                {
                    new IndexField("testIndexKey2", false)
                }
            ),
        }
    );

    static readonly GetCustomIdsResponse k_GetCustomIdsResponse = new(
        new List<GetCustomIdsResponseResultsInner>
        {
            new(
                "testId1",
                new AccessClassesWithMetadata(
                    null, new AccessClassMetadata(1, 100), null, new AccessClassMetadata(2, 200))),
            new(
                "testId1",
                new AccessClassesWithMetadata(
                    null, new AccessClassMetadata(1, 100), null, new AccessClassMetadata(2, 200))),
        },
        new GetPlayersWithDataResponseLinks("someLink")
    );

    static readonly List<QueryIndexResponseResultsInner> k_ValidQueryResponseList = new List<QueryIndexResponseResultsInner>()
    {
        new QueryIndexResponseResultsInner(
            "id",
            new List<Item>()
            {
                new Item(
                    "key1",
                    "value",
                    "writelock",
                    new ModifiedMetadata(DateTime.Now),
                    new ModifiedMetadata(DateTime.Today))
            }
        )
    };

    static readonly QueryIndexResponse k_ValidQueryResponse = new QueryIndexResponse
    {
        Results = k_ValidQueryResponseList
    };

    static readonly List<IndexField> k_ValidIndexFields = new List<IndexField>()
    {
        new IndexField("key1", true),
        new IndexField("key2", false)
    };

    static readonly CreateIndexBody k_ValidCreatePlayerIndexBody = new CreateIndexBody (
        new CreateIndexBodyIndexConfig(k_ValidIndexFields));

    static readonly CreateIndexResponse k_ValidCreateIndexResponse = new CreateIndexResponse("id", IndexStatus.READY);
    
    public async Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        var cloudsaveServiceModels = await MappingModelUtils.ParseMappingModelsFromGeneratorConfigAsync("cloud-save-api-v1-generator-config.yaml", new());
        return cloudsaveServiceModels.ToArray();
    }

    static void MockListIndexes(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create()
                .WithPath($"{k_BaseUrl}/indexes")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(k_GetIndexIdsResponse)
                .WithStatusCode(HttpStatusCode.OK));
    }

    static void MockListCustomIds(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create()
                .WithPath($"{k_BaseUrl}/custom")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(k_GetCustomIdsResponse)
                .WithStatusCode(HttpStatusCode.OK));
    }

    static void MockPlayerQueries(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create()
                .WithPath($"{k_BaseUrl}/indexes/players")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(k_ValidQueryResponse)
                .WithStatusCode(HttpStatusCode.OK));
        mockServer.Given(Request.Create()
                .WithPath($"{k_BaseUrl}/indexes/players/public")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(k_ValidQueryResponse)
                .WithStatusCode(HttpStatusCode.OK));
        mockServer.Given(Request.Create()
                .WithPath($"{k_BaseUrl}/indexes/players/protected")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(k_ValidQueryResponse)
                .WithStatusCode(HttpStatusCode.OK));
    }

    static void MockCustomDataQueries(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create()
                .WithPath($"{k_BaseUrl}/custom/query")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(k_ValidQueryResponse)
                .WithStatusCode(HttpStatusCode.OK));
        mockServer.Given(Request.Create()
                .WithPath($"{k_BaseUrl}/custom/private/query")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(k_ValidQueryResponse)
                .WithStatusCode(HttpStatusCode.OK));
    }

    static void MockCreatePlayerIndexes(WireMockServer mockServer)
    {
        mockServer.Given(Request.Create()
                .WithPath($"{k_BaseUrl}/indexes/players")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(k_ValidCreateIndexResponse)
                .WithStatusCode(HttpStatusCode.OK));
        mockServer.Given(Request.Create()
                .WithPath($"{k_BaseUrl}/indexes/players/protected")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(k_ValidCreateIndexResponse)
                .WithStatusCode(HttpStatusCode.OK));
        mockServer.Given(Request.Create()
                .WithPath($"{k_BaseUrl}/indexes/players/public")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(k_ValidCreateIndexResponse)
                .WithStatusCode(HttpStatusCode.OK));
    }

    public void CustomMock(WireMockServer mockServer)
    {
        MockListIndexes(mockServer);
        MockListCustomIds(mockServer);
        MockPlayerQueries(mockServer);
        MockCustomDataQueries(mockServer);
        MockCreatePlayerIndexes(mockServer);
        mockServer.AllowPartialMapping();
    }
}
