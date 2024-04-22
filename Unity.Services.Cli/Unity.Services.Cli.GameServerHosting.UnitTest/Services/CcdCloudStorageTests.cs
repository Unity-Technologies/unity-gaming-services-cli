using System.Net;
using System.Text;
using Moq;
using Moq.Protected;
using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Api;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;
using Unity.Services.Multiplay.Authoring.Core.Builds;
using HttpClient = System.Net.Http.HttpClient;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Services;

public class CcdCloudStorageTests
{
    CcdCloudStorageClient? m_CcdCloudStorage;

    Mock<IBucketsApi>? m_MockBucketsApi;
    Mock<IEntriesApi>? m_MockEntriesApi;
    Mock<HttpMessageHandler>? m_MockMessageHandler;

    [SetUp]
    public void SetUp()
    {
        m_MockBucketsApi = new Mock<IBucketsApi>(MockBehavior.Strict);
        m_MockEntriesApi = new Mock<IEntriesApi>(MockBehavior.Strict);
        m_MockMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        m_CcdCloudStorage = new CcdCloudStorageClient(
            m_MockBucketsApi.Object,
            m_MockEntriesApi.Object,
            new HttpClient(m_MockMessageHandler.Object),
            new GameServerHostingApiConfig());
    }

    [Test]
    public async Task FindBucket_LoadsBucketByName()
    {
        var bucketId = Guid.NewGuid();
        var bucketName = "bucket";
        m_MockBucketsApi!.Setup(api => api.ListBucketsByProjectEnvAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new List<CcdGetBucket200Response> { new(id: bucketId, name: bucketName) }));

        var bucket = await m_CcdCloudStorage!.FindBucket("test");

        Assert.That(bucket.ToGuid(), Is.EqualTo(bucketId));
    }

    [Test]
    public async Task CreateBucket_CreatesNewBucket()
    {
        var bucketId = Guid.NewGuid();
        var bucketName = "bucket";
        m_MockBucketsApi!.Setup(api => api.CreateBucketByProjectEnvAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CcdCreateBucketByProjectRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new CcdGetBucket200Response(id: bucketId, name: bucketName)));

        var bucket = await m_CcdCloudStorage!.CreateBucket("test");

        Assert.That(bucket.ToGuid(), Is.EqualTo(bucketId));
    }

    [Test]
    public async Task UploadBuildEntries_UploadsNewEntries()
    {
        SetupUpload(new List<CcdCreateOrUpdateEntryBatch200ResponseInner>());

        await m_CcdCloudStorage!.UploadBuildEntries(
            new CloudBucketId { Id = Guid.NewGuid() },
            new List<BuildEntry>
            {
                new ("path", new MemoryStream(Encoding.UTF8.GetBytes("content")))
            },
            onUpdated: _ => { }
        );

        m_MockEntriesApi!.Verify(api => api.CreateOrUpdateEntryByPathEnvAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CcdCreateOrUpdateEntryByPathRequest>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()));
    }

    [Test]
    public async Task UploadBuildEntries_WhenExactFileExists_DoesNotUpload()
    {
        SetupUpload(new List<CcdCreateOrUpdateEntryBatch200ResponseInner>
        {
            // Uppercase hash to ensure that case differences are handled
            new (path: "path", contentHash: "9a0364b9e99bb480dd25e1f0284c8555".ToUpperInvariant())
        });

        await m_CcdCloudStorage!.UploadBuildEntries(
            new CloudBucketId { Id = Guid.NewGuid() },
            new List<BuildEntry>
            {
                new ("path", new MemoryStream(Encoding.UTF8.GetBytes("content")))
            },
            onUpdated: _ => { });

        m_MockEntriesApi!.Verify(api => api.CreateOrUpdateEntryByPathEnvAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CcdCreateOrUpdateEntryByPathRequest>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Test]
    public async Task UploadBuildEntries_WhenOrphanExists_DeletesOrphan()
    {
        SetupUpload(new List<CcdCreateOrUpdateEntryBatch200ResponseInner>
        {
            new (path: "path", contentHash: "hash")
        });

        await m_CcdCloudStorage!.UploadBuildEntries(
            new CloudBucketId { Id = Guid.NewGuid() },
            new List<BuildEntry>(),
            onUpdated: _ => { });

        m_MockEntriesApi!.Verify(api => api.DeleteEntryEnvAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()));
    }

    void SetupUpload(List<CcdCreateOrUpdateEntryBatch200ResponseInner> ccdEntries)
    {
        m_MockEntriesApi!.Setup(api => api.GetEntriesEnvAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<Guid?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(ccdEntries));
        m_MockEntriesApi.Setup(api => api.CreateOrUpdateEntryByPathEnvAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CcdCreateOrUpdateEntryByPathRequest>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new CcdCreateOrUpdateEntryBatch200ResponseInner(signedUrl: "https://signed.url.example.com")));
        m_MockEntriesApi.Setup(api => api.DeleteEntryEnvAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new List<object>()));
        m_MockMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
    }
}
