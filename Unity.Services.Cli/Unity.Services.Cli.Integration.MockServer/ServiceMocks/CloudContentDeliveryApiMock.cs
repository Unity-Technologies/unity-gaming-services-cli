using System.Collections;
using Unity.Services.Cli.MockServer;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;
using WireMock.Admin.Mappings;
using WireMock.Matchers;
using WireMock.Server;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Types;

namespace Unity.Services.Cli.MockServer.ServiceMocks;

public class CloudContentDeliveryApiMock : IServiceApiMock
{

    static readonly string k_Uuid = "00000000-0000-0000-0000-000000000000";

    static readonly CcdGetAllByBucket200ResponseInner k_Permission = new(
        "write",
        "allow",
        "bucket/00000000-0000-0000-0000-000000000000",
        "user"
    );

    static readonly CcdPromoteBucketAsync200Response k_Promote = new(
        new Guid("00000000-0000-0000-0000-000000000000"));

    static readonly CcdGetPromotions200ResponseInner k_Promotion = new(
        "",
        new Guid("00000000-0000-0000-0000-000000000000"),
        "my bucket name",
        new Guid("00000000-0000-0000-0000-000000000000"),
        "production",
        new Guid("00000000-0000-0000-0000-000000000000"),
        1,
        new Guid("00000000-0000-0000-0000-000000000000"),
        CcdGetPromotions200ResponseInner.PromotionStatusEnum.Complete);

    static readonly CcdGetBucket200ResponseLastReleaseBadgesInner k_Badge =
        new()
        {
            Name = "badge 1",
            Releaseid = new Guid(k_Uuid),
            Releasenum = 1
        };

    static readonly List<CcdGetBucket200ResponseLastReleaseBadgesInner> k_ListBadge =
        new()
        {
            new CcdGetBucket200ResponseLastReleaseBadgesInner
            {
                Name = "badge 1",
                Releaseid = new Guid(k_Uuid),
                Releasenum = 1
            },
            new CcdGetBucket200ResponseLastReleaseBadgesInner
            {
                Name = "badge 2",
                Releaseid = new Guid(k_Uuid),
                Releasenum = 2
            }
        };

    static readonly CcdGetBucket200ResponseLastRelease k_Release = new()
    {
        Releaseid = new Guid(k_Uuid),
        Releasenum = 1,
        Badges = k_ListBadge,
        Notes = "my note",
        PromotedFromBucket = new Guid(k_Uuid),
        PromotedFromRelease = new Guid(k_Uuid)
    };

    static readonly CcdCreateOrUpdateEntryBatch200ResponseInner k_Entry = new()
    {
        Complete = true,
        ContentHash = "ac043a397e20f96d5ddffb8b16d5defd",
        ContentLink =
            "https://00000000-0000-0000-0000-000000000000.client-api.unity3dusercontent.com/client_api/v1/environments/00000000-0000-0000-0000-000000000000/buckets/00000000-0000-0000-0000-000000000000/entries/00000000-0000-0000-0000-000000000000/versions/00000000-0000-0000-0000-000000000000/content/",
        ContentSize = 2692709,
        ContentType = "image/jpeg",
        CurrentVersionid = new Guid(k_Uuid),
        Entryid = new Guid(k_Uuid),
        Labels = new List<string>()
        {
            "my label"
        },
        Metadata = "{}",
        Path = "image.jpg",
        SignedUrl = "http://localhost:8080/ccd/upload"
    };

    static readonly CcdGetBucket200Response k_BucketResponse = new()
    {
        Id = new Guid("00000000-0000-0000-0000-000000000000"),
        Name = "test abc",
        Description = "my description",
        EnvironmentName = "production",
        Projectguid = new Guid(CommonKeys.ValidProjectId),
        Private = true
    };

    static readonly List<CcdCreateOrUpdateEntryBatch200ResponseInner> k_Entries = new()
    {
        k_Entry
    };

    public Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        IReadOnlyList<MappingModel> models = new List<MappingModel>();
        return Task.FromResult(models);
    }

    public void CustomMock(WireMockServer mockServer)
    {
        // Define the response headers
        var responseHeaders = new Dictionary<string, WireMockList<string>>
        {
            { "Content-Type", new WireMockList<string>("application/json") },
            { "Content-Range", new WireMockList<string>("items 1-10/10") },
            {
                "unity-ratelimit",
                new WireMockList<string>("limit=40,remaining=39,reset=1;limit=100000,remaining=99999,reset=1800")
            }
        };

        MockGetAllBuckets(mockServer, responseHeaders);
        MockGetBucket(mockServer, responseHeaders);
        MockDeleteBucket(mockServer, responseHeaders);
        MockPostBucket(mockServer, responseHeaders);
        MockGetBucketPermission(mockServer, responseHeaders);
        MockPutBucketPermission(mockServer, responseHeaders);
        MockPostBucketPermission(mockServer, responseHeaders);

        MockListReleases(mockServer, responseHeaders);
        MockGetRelease(mockServer, responseHeaders);
        MockUpdateRelease(mockServer, responseHeaders);
        MockCreateRelease(mockServer, responseHeaders);

        MockGetAllBadges(mockServer, responseHeaders);
        MockCreateBadge(mockServer, responseHeaders);
        MockDeleteBadge(mockServer, responseHeaders);

        MockPostPromoteBucket(mockServer, responseHeaders);
        MockGetPromotionStatus(mockServer, responseHeaders);

        MockGetEntry(mockServer, responseHeaders);
        MockGetAllEntries(mockServer, responseHeaders);
        MockPutEntry(mockServer, responseHeaders);
        MockCreateOrUpdateEntries(mockServer, responseHeaders);
        MockCreateOrUpdateEntry(mockServer, responseHeaders);
        MockDeleteEntry(mockServer, responseHeaders);

        MockUploadContent(mockServer, responseHeaders);
    }

    static ArrayList MockGetAllBuckets(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        var listBucket = new ArrayList();

        listBucket.Add(
            new CcdGetBucket200Response()
            {
                Id = new Guid("00000000-0000-0000-0000-000000000000"),
                Name = CommonKeys.ValidBucketName,
                Description = "my description",
                EnvironmentName = "production",
                Projectguid = new Guid(CommonKeys.ValidProjectId),
                Private = true
            });

        listBucket.Add(
            new CcdGetBucket200Response()
            {
                Id = new Guid("00000000-0000-0000-0000-000000000000"),
                Name = "android",
                Description = "my description",
                EnvironmentName = "production",
                Projectguid = new Guid(CommonKeys.ValidProjectId),
                Private = true
            });

        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(listBucket)
                    .WithStatusCode(200));

        return listBucket;
    }

    static void MockGetAllBadges(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*/badges")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_ListBadge)
                    .WithStatusCode(200));
    }

    static void MockCreateBadge(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*/badges")
                    .UsingPut())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_Badge)
                    .WithStatusCode(200));
    }

    static void MockDeleteBadge(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*/badges/*")
                    .UsingDelete())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithStatusCode(204));
    }

    static void MockListReleases(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        var list = new List<CcdGetBucket200ResponseLastRelease>();
        list.Add(k_Release);
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*/releases")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(list)
                    .WithStatusCode(200));
    }

    static void MockGetRelease(WireMockServer mockServer, Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*/releases/*")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_Release)
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
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*/releases")
                    .UsingPost())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_Release)
                    .WithStatusCode(200));
    }

    static void MockUpdateRelease(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*/releases/*")
                    .UsingPut())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_Release)
                    .WithStatusCode(200));
    }

    static void MockGetBucket(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_BucketResponse)
                    .WithStatusCode(200));
    }

    static void MockGetBucketPermission(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        var list = new List<CcdGetAllByBucket200ResponseInner>();

        list.Add(
            new CcdGetAllByBucket200ResponseInner(
                "1",
                "1",
                "bucket/00000000-0000-0000-0000-000000000000",
                "user"
            ));
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*/permissions")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(list)
                    .WithStatusCode(200));
    }

    static void MockPostBucketPermission(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*/permissions")
                    .UsingPost())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_Permission)
                    .WithStatusCode(200));
    }

    static void MockPutBucketPermission(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*/permissions")
                    .UsingPut())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_Permission)
                    .WithStatusCode(200));

    }

    static void MockDeleteBucket(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*")
                    .UsingDelete())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithStatusCode(201));
    }

    static void MockPostBucket(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets")
                    .UsingPost())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_BucketResponse)
                    .WithStatusCode(200));
    }

    static void MockPostPromoteBucket(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/*/buckets/*/promoteasync")
                    .UsingPost())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_Promote)
                    .WithStatusCode(200));
    }

    static void MockGetPromotionStatus(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*/promote/*")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_Promotion)
                    .WithStatusCode(200));
    }

    static void MockGetAllEntries(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithParam("starting_after", "00000000-0000-0000-0000-000000000000")
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*/entries")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(new List<CcdCreateOrUpdateEntryBatch200ResponseInner>())
                    .WithStatusCode(200));

        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*/entries")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_Entries)
                    .WithStatusCode(200));

    }

    static void MockGetEntry(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*/entries/*")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_Entry)
                    .WithStatusCode(200));
    }

    static void MockCreateOrUpdateEntry(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*/entry_by_path")
                    .UsingPost())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_Entry)
                    .WithStatusCode(200));
    }

    static void MockCreateOrUpdateEntries(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {

        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*/batch/entries")
                    .UsingPost())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_Entries)
                    .WithStatusCode(200));
    }

    static void MockPutEntry(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*/entries/*")
                    .UsingPut())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_Entry)
                    .WithStatusCode(200));
    }

    static void MockDeleteEntry(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"/ccd/management/v1/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/buckets/*/batch/delete/entries")
                    .UsingDelete())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithStatusCode(204));
    }

    static void MockUploadContent(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath("/ccd/upload")
                    .UsingPut())
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithStatusCode(200));
    }

}
