using System.Diagnostics;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudContentDelivery.Model;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Api;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace CloudContentDeliveryTest.Service;

[TestFixture]
public class SynchronizationServiceTests
{
    SynchronizationService m_SyncService = null!;
    readonly Mock<IEntriesApi> m_EntriesApiMock = new();
    readonly Mock<IUploadContentClient> m_UploadContentClientMock = new();
    IFileSystem m_FileSystemMock = null!;
    readonly Mock<IServiceAccountAuthenticationService> m_AuthServiceMock = new();
    readonly Mock<IContentDeliveryValidator> m_ContentValidatorMock = new();
    readonly Mock<ILogger<SynchronizationService>> m_LoggerMock = new();
    const string k_TestAccessToken = "test-token";
    const string k_ContentHash = "0e5a0d2fa6bbd6e7b5b2a8337ebb4733";
    const long k_ContentSize = 7;
    const string k_ContentType = "type";

    [SetUp]
    public void Setup()
    {

        var file1 = new MockFileData("Content of file 1");
        var file2 = new MockFileData("Content of file 2");
        var file3 = new MockFileData("Content of file 3");

        var fileSystemEntries = new Dictionary<string, MockFileData>
        {
            { @"local-folder/file1.txt", file1 },
            { @"local-folder/file2.txt", file2 },
            { @"local-folder/file3.txt", file3 },
            {
                @"images/image1.png", new MockFileData(
                    new byte[]
                    {
                        0x89,
                        0x50,
                        0x4E,
                        0x47
                    })
            },
            {
                @"data/binaryFile.bin", new MockFileData(
                    new byte[]
                    {
                        0x00,
                        0x01,
                        0x02,
                        0x03,
                        0x04
                    })
            }
        };

        m_FileSystemMock = new MockFileSystem(fileSystemEntries);
        m_AuthServiceMock.Reset();
        m_EntriesApiMock.Reset();

        m_EntriesApiMock.Setup(api => api.Configuration).Returns(new Configuration());
        m_AuthServiceMock.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .Returns(Task.FromResult(k_TestAccessToken));
        var headers = new Multimap<string, string>();
        headers.Add("Content-Range", "items 1-10/500");
        headers.Add("unity-ratelimit", "limit=40,remaining=39,reset=1;limit=100000,remaining=99999,reset=1800");


        var ccdCreateOrUpdateList = new List<CcdCreateOrUpdateEntryBatch200ResponseInner>();
        ccdCreateOrUpdateList.Add(new CcdCreateOrUpdateEntryBatch200ResponseInner { SignedUrl = "url", Path = "file1.txt" });
        m_EntriesApiMock.Setup(
                api => api.CreateOrUpdateEntryBatchEnvWithHttpInfoAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<List<CcdCreateOrUpdateEntryBatchRequestInner>>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ApiResponse<List<CcdCreateOrUpdateEntryBatch200ResponseInner>>(
                    HttpStatusCode.OK,
                    headers,
                    ccdCreateOrUpdateList)
            );

        m_EntriesApiMock.Setup(
                api => api.DeleteEntryBatchEnvWithHttpInfoAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<List<CcdDeleteEntryBatchRequestInner>>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ApiResponse<object>(
                    HttpStatusCode.NoContent,
                    headers,
                    new object())
            );

        m_UploadContentClientMock.Setup(client => client.GetContentType(It.IsAny<string>())).Returns(k_ContentType);
        m_UploadContentClientMock.Setup(client => client.GetContentSize(It.IsAny<FileSystemStream>()))
            .Returns(k_ContentSize);
        m_UploadContentClientMock.Setup(client => client.GetContentHash(It.IsAny<FileSystemStream>()))
            .Returns(k_ContentHash);

        m_SyncService = new SynchronizationService(
            m_EntriesApiMock.Object,
            m_UploadContentClientMock.Object,
            m_FileSystemMock,
            m_AuthServiceMock.Object,
            m_ContentValidatorMock.Object);
    }

    [Test]
    public async Task CalculateSynchronization_ValidInput_ReturnsSyncResult()
    {
        var headers = new Multimap<string, string>();
        headers.Add("Content-Range", "items 1-10/500");
        headers.Add("unity-ratelimit", "limit=40,remaining=39,reset=1;limit=100000,remaining=99999,reset=1800");
        var expectedEntriesList = new ApiResponse<List<CcdCreateOrUpdateEntryBatch200ResponseInner>>(
            HttpStatusCode.OK,
            headers,
            new List<CcdCreateOrUpdateEntryBatch200ResponseInner>());

        m_EntriesApiMock.Setup(
                api => api.GetEntriesEnvWithHttpInfoAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    It.IsAny<long?>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<int?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(expectedEntriesList);

        var result = await m_SyncService.CalculateSynchronization(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            ".",
            "",
            false,
            CloudContentDeliveryTestsConstants.Labels,
            CloudContentDeliveryTestsConstants.Metadata,
            CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.That(result.EntriesToAdd.Count, Is.EqualTo(5));
        Assert.That(result.EntriesToUpdate, Is.Not.Null.And.Empty);
        Assert.That(result.EntriesToDelete, Is.Not.Null.And.Empty);
        Assert.That(result.EntriesToSkip, Is.Not.Null.And.Empty);
    }

    [Test]
    public void CalculateUploadSpeed_ValidInput_ReturnsUploadSpeed()
    {
        var result = SynchronizationService.CalculateUploadSpeed(60, 60);
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void CalculateOperationSummary_ValidInput_ReturnsOperationSummary()
    {
        var syncResult = new SyncResult(
        );
        var syncProcessTime = 2.2;

        var result = SynchronizationService.CalculateOperationSummary(
            true,
            syncResult,
            syncProcessTime,
            null,
            null);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.OperationCompletedSuccessfully);
    }

    [Test]
    public void GetFilesFromDir_InvalidDirectory_ThrowsCliException()
    {
        var directoryPath = "test-directory";
        var exclusionPattern = ".jpg";
        Assert.Throws<CliException>(() => m_SyncService.GetFilesFromDir(directoryPath, exclusionPattern));
    }

    [Test]
    public void GetFilesFromDir_ValidInput_ReturnsEmptyFileSet()
    {
        var directoryPath = "local-folder";
        var exclusionPattern = ".txt";

        var result = m_SyncService.GetFilesFromDir(directoryPath, exclusionPattern);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetFilesFromDir_ValidInput_ReturnsFileSet()
    {
        var directoryPath = "local-folder";
        var exclusionPattern = ".jpg";

        var result = m_SyncService.GetFilesFromDir(directoryPath, exclusionPattern);

        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task AuthorizeServiceAsync_ValidInput_AuthorizesService()
    {
        m_AuthServiceMock.Setup(a => a.GetAccessTokenAsync(CancellationToken.None)).ReturnsAsync("test-token");
        await m_SyncService.AuthorizeServiceAsync(CancellationToken.None);
        m_AuthServiceMock.Verify(a => a.GetAccessTokenAsync(CancellationToken.None), Times.Once);
        Assert.That(
            m_EntriesApiMock.Object.Configuration.DefaultHeaders["Authorization"],
            Is.EqualTo("Basic test-token"));
    }

    [Test]
    public void CalculateDifference_ValidInput_FileAdded()
    {
        var localFiles = new HashSet<string>
        {
            "file1.txt"
        };
        var remoteEntries = new List<CcdCreateOrUpdateEntryBatch200ResponseInner>();

        var result = m_SyncService.CalculateDifference(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            "local-folder",
            true,
            remoteEntries,
            localFiles,
            CloudContentDeliveryTestsConstants.Labels,
            CloudContentDeliveryTestsConstants.Metadata
        );
        Assert.That(result.EntriesToAdd.Count, Is.EqualTo(1), "EntriesToAdd is equal to 1");
    }

    [Test]
    public void CalculateDifference_ValidInput_FileSkipped()
    {
        var localFiles = new HashSet<string>
        {
            "file1.txt"
        };
        var remoteEntries = new List<CcdCreateOrUpdateEntryBatch200ResponseInner>
        {
            new()
            {
                Path = "file1.txt",
                ContentSize = k_ContentSize,
                ContentType = k_ContentType,
                ContentHash = k_ContentHash,
                Entryid = new Guid(),
                CurrentVersionid = new Guid()
            }
        };

        var result = m_SyncService.CalculateDifference(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            "local-folder",
            true,
            remoteEntries,
            localFiles,
            CloudContentDeliveryTestsConstants.Labels,
            CloudContentDeliveryTestsConstants.Metadata
        );
        Assert.That(result.EntriesToSkip.Count, Is.EqualTo(1), "Skip is equal to 1");
    }

    [Test]
    public void CalculateDifference_ValidInput_FileUpdated()
    {
        var localFiles = new HashSet<string>
        {
            "file1.txt"
        };
        var remoteEntries = new List<CcdCreateOrUpdateEntryBatch200ResponseInner>
        {
            new()
            {
                Path = "file1.txt",
                ContentSize = 52,
                ContentType = k_ContentType,
                ContentHash = "d3a1d1c92b50629f1b9253a32979b682",
                Entryid = new Guid(),
                CurrentVersionid = new Guid()
            }
        };

        var result = m_SyncService.CalculateDifference(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            "local-folder",
            false,
            remoteEntries,
            localFiles,
            CloudContentDeliveryTestsConstants.Labels,
            CloudContentDeliveryTestsConstants.Metadata
        );
        Assert.That(result.EntriesToUpdate.Count, Is.EqualTo(1), "Update is equal to 1");
    }

    [Test]
    public void CalculateDifference_ValidInput_FileDeleted()
    {
        var localFiles = new HashSet<string>();
        var remoteEntries = new List<CcdCreateOrUpdateEntryBatch200ResponseInner>
        {
            new()
            {
                Path = "file1.txt",
                ContentSize = k_ContentSize,
                ContentType = k_ContentType,
                ContentHash = k_ContentHash,
                Entryid = new Guid(),
                CurrentVersionid = new Guid()
            }
        };

        var result = m_SyncService.CalculateDifference(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            "local-folder",
            true,
            remoteEntries,
            localFiles,
            CloudContentDeliveryTestsConstants.Labels,
            CloudContentDeliveryTestsConstants.Metadata
        );
        Assert.That(result.EntriesToDelete.Count, Is.EqualTo(1), "Delete is equal to 1");
    }

    [Test]
    public async Task ProcessSynchronization_SuccessfulSynchronization_ReturnsReleaseEntries()
    {
        m_UploadContentClientMock.Setup(
                client => client.UploadContentToCcd(
                    It.IsAny<string>(),
                    It.IsAny<FileSystemStream>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var syncResult = m_SyncService.CalculateDifference(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            "local-folder",
            true,
            new List<CcdCreateOrUpdateEntryBatch200ResponseInner>
            {
                new()
                {
                    Entryid = new Guid(),
                    Path = "file2.txt",
                    ContentHash = "e1b1ec4ec8057de9b2a72c38ba0305c5",
                    ContentSize = 8,
                    ContentType = "text/plain",
                    Complete = true
                },
                new()
                {
                    Entryid = new Guid(),
                    Path = "file3.txt",
                    ContentSize = k_ContentSize,
                    ContentType = k_ContentType,
                    ContentHash = k_ContentHash,
                    Complete = true
                },
                new()
                {
                    Entryid = new Guid(),
                    Path = "file5.txt",
                    ContentHash = "h5gf1ec4ec457de9b2k34c38ba0305c5",
                    ContentSize = 8,
                    ContentType = "text/plain",
                    Complete = true
                }
            },
            new HashSet<string>
            {
                "file1.txt",
                "file2.txt",
                "file3.txt"
            },
            CloudContentDeliveryTestsConstants.Labels,
            CloudContentDeliveryTestsConstants.Metadata
        );

        Assert.That(syncResult.EntriesToAdd.Count, Is.EqualTo(1), "Add is equal to 1");
        Assert.That(syncResult.EntriesToDelete.Count, Is.EqualTo(1), "Delete is equal to 1");
        Assert.That(syncResult.EntriesToUpdate.Count, Is.EqualTo(1), "Update is equal to 1");
        Assert.That(syncResult.EntriesToSkip.Count, Is.EqualTo(1), "Skip is equal to 1");

        var result = await m_SyncService.ProcessSynchronization(
            m_LoggerMock.Object,
            true,
            syncResult,
            "local-folder",
            3,
            5,
            1000,
            CancellationToken.None);

        Assert.That(result.Count, Is.EqualTo(3), "process result is of size 3");

    }

    [Test]
    public void ProcessSynchronization_TaskThrowsException_ThrowsCliException()
    {
        var syncResult = m_SyncService.CalculateDifference(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            "local-folder",
            false,
            new List<CcdCreateOrUpdateEntryBatch200ResponseInner>(),
            new HashSet<string>
            {
                "file1.txt",
                "file2.txt"
            },
            CloudContentDeliveryTestsConstants.Labels,
            CloudContentDeliveryTestsConstants.Metadata
        );

        m_UploadContentClientMock.Setup(
                client => client.UploadContentToCcd(
                    It.IsAny<string>(),
                    It.IsAny<FileSystemStream>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        Assert.ThrowsAsync<CliException>(
            () => m_SyncService.ProcessSynchronization(
                m_LoggerMock.Object,
                true,
                syncResult,
                "local-folder",
                3,
                5,
                1000,
                CancellationToken.None));
    }

    [Test]
    public async Task ShouldNotDelayWhenRateLimitNotExceeded()
    {
        var headers = new Multimap<string, string>
        {
            { "unity-ratelimit", "limit=40,remaining=39,reset=1" }
        };
        var rateLimitStatus = new SharedRateLimitStatus();
        var cancellationToken = new CancellationToken(false);

        await SynchronizationService.RespectRateLimitAsync(headers, rateLimitStatus, cancellationToken);

        Assert.IsFalse(rateLimitStatus.IsRateLimited);
        Assert.That(rateLimitStatus.ResetTime, Is.EqualTo(TimeSpan.Zero), "Reset time is equal to 0");

    }

    [Test]
    public async Task ShouldDelayWhenRateLimitExceeded()
    {
        var headers = new Multimap<string, string>
        {
            { "unity-ratelimit", "limit=40,remaining=0,reset=1" }
        };
        var rateLimitStatus = new SharedRateLimitStatus();
        var cancellationToken = new CancellationToken(false);
        var stopwatch = Stopwatch.StartNew();
        await SynchronizationService.RespectRateLimitAsync(headers, rateLimitStatus, cancellationToken);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds > 700);
        stopwatch.Stop();
        Assert.IsFalse(rateLimitStatus.IsRateLimited);
        Assert.That(rateLimitStatus.ResetTime, Is.EqualTo(TimeSpan.Zero), "Reset time is equal to 0");

    }

    [Test]
    public void ShouldUpdateRateLimitStatusBeforeDelayOnRateLimitExceeded()
    {
        var headers = new Multimap<string, string>
        {
            { "unity-ratelimit", "limit=40,remaining=0,reset=60" }
        };
        var rateLimitStatus = new SharedRateLimitStatus();

#pragma warning disable CS4014 // Because we don't want to await
        SynchronizationService.RespectRateLimitAsync(headers, rateLimitStatus, CancellationToken.None);
#pragma warning restore CS4014

        Assert.IsTrue(rateLimitStatus.IsRateLimited);
        Assert.That(
            rateLimitStatus.ResetTime,
            Is.EqualTo(TimeSpan.FromSeconds(60)),
            "Reset time is equal to 60 seconds");

    }

}
