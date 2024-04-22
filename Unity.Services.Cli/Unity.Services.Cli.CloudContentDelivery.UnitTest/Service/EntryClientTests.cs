using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Api;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;
using Configuration = Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client.Configuration;

namespace CloudContentDeliveryTest.Service;

[TestFixture]
public class EntryClientTests
{
    readonly Mock<IServiceAccountAuthenticationService> m_AuthServiceMock = new();
    readonly Mock<IEntriesApi> m_EntriesApiMock = new();
    readonly Mock<IContentApi> m_ContentApiMock = new();
    readonly Mock<IContentDeliveryValidator> m_ContentDeliveryValidator = new();
    readonly Mock<HttpClient> m_HttpClient = new();
    readonly Mock<IUploadContentClient> m_UploadContentClient = new();

    IFileSystem m_FileSystemMock = null!;
    EntryClient m_EntryClient = null!;

    const string k_TestAccessToken = "test-token";

    [SetUp]
    public void SetUp()
    {
        m_FileSystemMock = new MockFileSystem(
            new Dictionary<string, MockFileData>
            {
                {
                    @"c:\folder\file.jpg", new MockFileData(
                        new byte[]
                        {
                            0x12,
                            0x34,
                            0x56,
                            0xd2
                        })
                },
                { @"local-folder/file1.txt", new MockFileData("Content of file 1") },
                { @"local-folder/file2.txt", new MockFileData("Content of file 2") }
            });

        m_AuthServiceMock.Reset();
        m_EntriesApiMock.Reset();
        m_ContentApiMock.Reset();
        m_HttpClient.Reset();
        m_UploadContentClient.Reset();
        m_EntriesApiMock.Setup(api => api.Configuration).Returns(new Configuration());
        m_ContentApiMock.Setup(api => api.Configuration).Returns(new Configuration());

        m_AuthServiceMock.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .Returns(Task.FromResult(k_TestAccessToken));

        m_EntryClient = new EntryClient(
            m_AuthServiceMock.Object,
            m_ContentDeliveryValidator.Object,
            m_EntriesApiMock.Object,
            m_ContentApiMock.Object,
            m_UploadContentClient.Object,
            m_FileSystemMock);

        m_UploadContentClient.Setup(client => client.GetContentType(It.IsAny<string>())).Returns("type");
        m_UploadContentClient.Setup(client => client.GetContentSize(It.IsAny<FileSystemStream>())).Returns(7);
        m_UploadContentClient.Setup(client => client.GetContentHash(It.IsAny<FileSystemStream>()))
            .Returns("hash");

        m_EntriesApiMock.Setup(
                x => x.GetEntryByPathEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.Path,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    It.IsAny<string>(),
                    0,
                    It.IsAny<CancellationToken>()
                ))
            .ReturnsAsync(
                new CcdCreateOrUpdateEntryBatch200ResponseInner
                {
                    Entryid = new Guid(CloudContentDeliveryTestsConstants.EntryId),
                    SignedUrl = "url"
                });
    }

    [Test]
    public async Task AuthorizeServiceAsync()
    {
        await m_EntryClient.AuthorizeServiceAsync(CancellationToken.None);
        m_AuthServiceMock.Verify(a => a.GetAccessTokenAsync(CancellationToken.None), Times.Once);
        Assert.Multiple(
            () =>
            {
                Assert.That(
                    m_EntriesApiMock.Object.Configuration.DefaultHeaders["Authorization"],
                    Is.EqualTo($"Basic {k_TestAccessToken}"));
            });
    }

    [Test]
    public async Task UpdateEntryAsync_Success()
    {
        var expectedResponse = new CcdCreateOrUpdateEntryBatch200ResponseInner
        {
            Entryid = new Guid(CloudContentDeliveryTestsConstants.EntryId)
        };

        m_EntriesApiMock.Setup(
                api => api.UpdateEntryEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.EntryId,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    It.IsAny<CcdUpdateEntryRequest>(),
                    0,
                    CancellationToken.None))
            .ReturnsAsync(expectedResponse);

        var result = await m_EntryClient.UpdateEntryAsync(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            CloudContentDeliveryTestsConstants.Path,
            CloudContentDeliveryTestsConstants.VersionId,
            CloudContentDeliveryTestsConstants.Labels,
            CloudContentDeliveryTestsConstants.Metadata);

        m_EntriesApiMock.Verify(
            api => api.UpdateEntryEnvAsync(
                CloudContentDeliveryTestsConstants.EnvironmentId,
                CloudContentDeliveryTestsConstants.BucketId,
                CloudContentDeliveryTestsConstants.EntryId,
                CloudContentDeliveryTestsConstants.ProjectId,
                It.IsAny<CcdUpdateEntryRequest>(),
                0,
                CancellationToken.None),
            Times.Once);

        Assert.IsNotNull(result);
        Assert.That(result!.Entryid, Is.EqualTo(expectedResponse.Entryid));
    }

    [Test]
    public void UpdateEntryAsync_ExceptionThrown()
    {

        m_EntriesApiMock.Setup(
                api => api.UpdateEntryEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.EntryId,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    It.IsAny<CcdUpdateEntryRequest>(),
                    0,
                    CancellationToken.None))
            .ThrowsAsync(new Exception("Error Updating Entry"));

        Assert.ThrowsAsync<Exception>(
            async () =>
            {
                await m_EntryClient.UpdateEntryAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.Path,
                    CloudContentDeliveryTestsConstants.VersionId,
                    CloudContentDeliveryTestsConstants.Labels,
                    CloudContentDeliveryTestsConstants.Metadata);
            });

    }

    [Test]
    public async Task GetEntryAsync_Success()
    {

        var result = await m_EntryClient.GetEntryAsync(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            CloudContentDeliveryTestsConstants.Path,
            CloudContentDeliveryTestsConstants.VersionId,
            CancellationToken.None);

        m_EntriesApiMock.Verify(
            api => api.GetEntryByPathEnvAsync(
                CloudContentDeliveryTestsConstants.EnvironmentId,
                CloudContentDeliveryTestsConstants.BucketId,
                CloudContentDeliveryTestsConstants.Path,
                CloudContentDeliveryTestsConstants.ProjectId,
                CloudContentDeliveryTestsConstants.VersionId,
                0,
                CancellationToken.None),
            Times.Once);

        Assert.IsNotNull(result);
        Assert.That(result!.Entryid, Is.EqualTo(new Guid(CloudContentDeliveryTestsConstants.EntryId)));
    }

    [Test]
    public async Task DeleteEntryAsync_Success()
    {
        m_EntriesApiMock.Setup(
            api => api.DeleteEntryEnvAsync(
                CloudContentDeliveryTestsConstants.EnvironmentId,
                CloudContentDeliveryTestsConstants.BucketId,
                CloudContentDeliveryTestsConstants.EntryId,
                CloudContentDeliveryTestsConstants.ProjectId,
                0,
                CancellationToken.None));

        var result = await m_EntryClient.DeleteEntryAsync(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            CloudContentDeliveryTestsConstants.Path);

        m_EntriesApiMock.Verify(
            api => api.DeleteEntryEnvAsync(
                CloudContentDeliveryTestsConstants.EnvironmentId,
                CloudContentDeliveryTestsConstants.BucketId,
                CloudContentDeliveryTestsConstants.EntryId,
                CloudContentDeliveryTestsConstants.ProjectId,
                0,
                CancellationToken.None),
            Times.Once);

        Assert.That(result, Is.EqualTo("Entry Deleted."));
    }

    [Test]
    public async Task CopyEntryAsync_Success()
    {
        var expectedResponse = new CcdCreateOrUpdateEntryBatch200ResponseInner
        {
            CurrentVersionid = new Guid(CloudContentDeliveryTestsConstants.VersionId),
            Entryid = new Guid(CloudContentDeliveryTestsConstants.EntryId),
            SignedUrl = "url"
        };

        m_EntriesApiMock.Setup(
                api => api.CreateOrUpdateEntryByPathEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    "local-folder/file1.txt",
                    CloudContentDeliveryTestsConstants.ProjectId,
                    It.IsAny<CcdCreateOrUpdateEntryByPathRequest>(),
                    true,
                    0,
                    CancellationToken.None))
            .ReturnsAsync(expectedResponse);

        m_EntriesApiMock.Setup(
                x => x.GetEntryByPathEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    "local-folder/file1.txt",
                    CloudContentDeliveryTestsConstants.ProjectId,
                    It.IsAny<string>(),
                    0,
                    It.IsAny<CancellationToken>()
                ))
            .ReturnsAsync(
                new CcdCreateOrUpdateEntryBatch200ResponseInner
                {
                    CurrentVersionid = new Guid(CloudContentDeliveryTestsConstants.VersionId),
                    Entryid = new Guid(CloudContentDeliveryTestsConstants.EntryId)
                });

        m_UploadContentClient.Setup(
                client => client.UploadContentToCcd(
                    It.IsAny<string>(),
                    It.IsAny<FileSystemStream>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var result = await m_EntryClient.CopyEntryAsync(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            "local-folder/file1.txt",
            "local-folder/file1.txt",
            CloudContentDeliveryTestsConstants.Labels,
            CloudContentDeliveryTestsConstants.Metadata);

        m_EntriesApiMock.Verify(
            api => api.CreateOrUpdateEntryByPathEnvAsync(
                CloudContentDeliveryTestsConstants.EnvironmentId,
                CloudContentDeliveryTestsConstants.BucketId,
                "local-folder/file1.txt",
                CloudContentDeliveryTestsConstants.ProjectId,
                It.IsAny<CcdCreateOrUpdateEntryByPathRequest>(),
                true,
                0,
                CancellationToken.None),
            Times.Once);

        Assert.Multiple(
            () =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result!.Entryid, Is.EqualTo(expectedResponse.Entryid));
                Assert.That(result.CurrentVersionid, Is.EqualTo(expectedResponse.CurrentVersionid));
            });
    }

    [Test]
    public Task CopyEntryAsync_UploadFails_ThrowsException()
    {
        var expectedResponse = new CcdCreateOrUpdateEntryBatch200ResponseInner
        {
            CurrentVersionid = new Guid(CloudContentDeliveryTestsConstants.VersionId),
            Entryid = new Guid(CloudContentDeliveryTestsConstants.EntryId),
            SignedUrl = "url"
        };

        m_EntriesApiMock.Setup(
                api => api.CreateOrUpdateEntryByPathEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    "local-folder/file1.txt",
                    CloudContentDeliveryTestsConstants.ProjectId,
                    It.IsAny<CcdCreateOrUpdateEntryByPathRequest>(),
                    true,
                    0,
                    CancellationToken.None))
            .ReturnsAsync(expectedResponse);

        m_UploadContentClient.Setup(
                client => client.UploadContentToCcd(
                    It.IsAny<string>(),
                    It.IsAny<FileSystemStream>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        Assert.ThrowsAsync<CliException>(
            () => m_EntryClient.CopyEntryAsync(
                CloudContentDeliveryTestsConstants.ProjectId,
                CloudContentDeliveryTestsConstants.EnvironmentId,
                CloudContentDeliveryTestsConstants.BucketId,
                "local-folder/file1.txt",
                "local-folder/file1.txt",
                CloudContentDeliveryTestsConstants.Labels,
                CloudContentDeliveryTestsConstants.Metadata));
        return Task.CompletedTask;
    }

    [Test]
    public async Task DownloadEntryAsync_Success_ReturnsContentStream()
    {
        var responseEntry = new CcdCreateOrUpdateEntryBatch200ResponseInner
        {
            Entryid = new Guid()
        };
        m_EntriesApiMock.Setup(
                api => api.GetEntryByPathEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.Path,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.VersionId,
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(responseEntry);

        var expectedStream = m_FileSystemMock.File.OpenRead(@"local-folder/file1.txt");
        m_ContentApiMock.Setup(
                api => api.GetContentEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    It.IsAny<string>(),
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.VersionId,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStream);

        var result = await m_EntryClient.DownloadEntryAsync(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            CloudContentDeliveryTestsConstants.Path,
            CloudContentDeliveryTestsConstants.VersionId,
            CancellationToken.None);

        Assert.That(result, Is.EqualTo(expectedStream));
    }

    [Test]
    public void DownloadEntryAsync_ApiFailure_ThrowsException()
    {
        var responseEntry = new CcdCreateOrUpdateEntryBatch200ResponseInner
        {
            Entryid = new Guid()
        };
        m_EntriesApiMock.Setup(
                api => api.GetEntryByPathEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.Path,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.VersionId,
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(responseEntry);

        m_ContentApiMock.Setup(
                api => api.GetContentEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    It.IsAny<string>(),
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.VersionId,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API failure"));

        Assert.ThrowsAsync<Exception>(
            () => m_EntryClient.DownloadEntryAsync(
                CloudContentDeliveryTestsConstants.ProjectId,
                CloudContentDeliveryTestsConstants.EnvironmentId,
                CloudContentDeliveryTestsConstants.BucketId,
                CloudContentDeliveryTestsConstants.Path,
                CloudContentDeliveryTestsConstants.VersionId,
                CancellationToken.None));
    }

    [Test]
    public void CopyEntryAsync_InvalidPath_ThrowsException()
    {

        Assert.ThrowsAsync<ArgumentException>(
            async () =>
                await m_EntryClient.CopyEntryAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    "",
                    "",
                    CloudContentDeliveryTestsConstants.Labels,
                    CloudContentDeliveryTestsConstants.Metadata));
    }

    [Test]
    public async Task ListEntryAsync_Success()
    {

        var expectedResponse = new ApiResponse<List<CcdCreateOrUpdateEntryBatch200ResponseInner>>(
            HttpStatusCode.OK,
            new Multimap<string, string>(),
            new List<CcdCreateOrUpdateEntryBatch200ResponseInner>
            {
                new(),
                new()
            }
        );

        m_EntriesApiMock.Setup(
                api => api.GetEntriesEnvWithHttpInfoAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    1,
                    new Guid(CloudContentDeliveryTestsConstants.StartingAfter),
                    10,
                    CloudContentDeliveryTestsConstants.Path,
                    CloudContentDeliveryTestsConstants.Label,
                    CloudContentDeliveryTestsConstants.ContentType,
                    CloudContentDeliveryTestsConstants.Complete,
                    CloudContentDeliveryTestsConstants.SortBy,
                    CloudContentDeliveryTestsConstants.SortOrder,
                    0,
                    CancellationToken.None))
            .ReturnsAsync(expectedResponse);

        var result = await m_EntryClient.ListEntryAsync(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            1,
            CloudContentDeliveryTestsConstants.StartingAfter,
            10,
            CloudContentDeliveryTestsConstants.Path,
            CloudContentDeliveryTestsConstants.Label,
            CloudContentDeliveryTestsConstants.ContentType,
            CloudContentDeliveryTestsConstants.Complete,
            CloudContentDeliveryTestsConstants.SortBy,
            CloudContentDeliveryTestsConstants.SortOrder,
            CancellationToken.None);

        m_EntriesApiMock.Verify(
            api => api.GetEntriesEnvWithHttpInfoAsync(
                CloudContentDeliveryTestsConstants.EnvironmentId,
                CloudContentDeliveryTestsConstants.BucketId,
                CloudContentDeliveryTestsConstants.ProjectId,
                1,
                new Guid(CloudContentDeliveryTestsConstants.StartingAfter),
                10,
                CloudContentDeliveryTestsConstants.Path,
                CloudContentDeliveryTestsConstants.Label,
                CloudContentDeliveryTestsConstants.ContentType,
                CloudContentDeliveryTestsConstants.Complete,
                CloudContentDeliveryTestsConstants.SortBy,
                CloudContentDeliveryTestsConstants.SortOrder,
                0,
                CancellationToken.None),
            Times.Once);

        Assert.IsNotNull(result);
        Assert.That(result.Data.Count(), Is.EqualTo(2));
    }
}
