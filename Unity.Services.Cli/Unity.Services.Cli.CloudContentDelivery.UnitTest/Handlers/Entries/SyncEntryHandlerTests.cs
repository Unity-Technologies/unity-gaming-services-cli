using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.CloudContentDelivery.Handlers.Entries;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Model;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.CloudContentDelivery.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace CloudContentDeliveryTest.Handlers.Entries;

[TestFixture]
public class SyncEntryHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<IEntryClient> m_MockEntryClient = new();
    readonly Mock<ILogger> m_MockLogger = new();
    readonly Mock<IClientWrapper> m_MockClientWrapper = new();
    readonly Mock<ISynchronizationService> m_MockSynchronizationService = new();

    [SetUp]
    public void Setup()
    {


        m_MockUnityEnvironment.Reset();
        m_MockEntryClient.Reset();
        m_MockLogger.Reset();

        m_MockClientWrapper.Setup(
                c =>
                    c.BucketClient!.GetBucketIdByName(
                        CloudContentDeliveryTestsConstants.ProjectId,
                        CloudContentDeliveryTestsConstants.EnvironmentId,
                        CloudContentDeliveryTestsConstants.BucketName,
                        CancellationToken.None))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.BucketId);
    }

    [Test]
    public async Task SyncEntryAsync_CallsLoadingIndicatorStartLoading()
    {
        var input = new CloudContentDeliveryInput();
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();
        var cancellationToken = CancellationToken.None;

        mockLoadingIndicator.Setup(
                li => li.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()))
            .Returns(Task.CompletedTask);
        await SyncEntryHandler.SyncEntriesAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockClientWrapper.Object,
            m_MockSynchronizationService.Object,
            m_MockLogger.Object,
            mockLoadingIndicator.Object,
            cancellationToken);
        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task SyncHandler_ValidInputLogsResult()
    {
        var cloudContentDeliveryInput = new CloudContentDeliveryInput
        {
            CloudProjectId = CloudContentDeliveryTestsConstants.ProjectId,
            BucketNameOpt = CloudContentDeliveryTestsConstants.BucketName,
            EntryPath = CloudContentDeliveryTestsConstants.Path,
            VersionId = CloudContentDeliveryTestsConstants.VersionId,
            LocalFolder = CloudContentDeliveryTestsConstants.LocalFolder,
            ExclusionPattern = CloudContentDeliveryTestsConstants.ExclusionPattern,
            Delete = CloudContentDeliveryTestsConstants.Delete,
            Retry = CloudContentDeliveryTestsConstants.Retry,
            DryRun = CloudContentDeliveryTestsConstants.DryRun,
            UpdateBadge = CloudContentDeliveryTestsConstants.UpdateBadge,
            CreateRelease = CloudContentDeliveryTestsConstants.CreateRelease,
            SyncMetadata = CloudContentDeliveryTestsConstants.Metadata,
            ReleaseNotes = CloudContentDeliveryTestsConstants.Notes,
            Labels = CloudContentDeliveryTestsConstants.Labels
        };

        var expectedSyncResult = new SyncResult();
        m_MockSynchronizationService.Setup(
                s => s.CalculateSynchronization(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.LocalFolder,
                    CloudContentDeliveryTestsConstants.ExclusionPattern,
                    CloudContentDeliveryTestsConstants.Delete,
                    CloudContentDeliveryTestsConstants.Labels,
                    CcdUtils.ParseMetadata(CloudContentDeliveryTestsConstants.Metadata),
                    CancellationToken.None))
            .ReturnsAsync(expectedSyncResult);

        m_MockSynchronizationService.Setup(
                s => s.ProcessSynchronization(
                    m_MockLogger.Object,
                    true,
                    expectedSyncResult,
                    CloudContentDeliveryTestsConstants.LocalFolder,
                    CloudContentDeliveryTestsConstants.Retry,
                    50,
                    100,
                    CancellationToken.None))
            .ReturnsAsync(new List<CcdCreateReleaseRequestEntriesInner>());

        m_MockClientWrapper.Setup(
                r => r.ReleaseClient!.CreateReleaseAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    It.IsAny<CcdCreateReleaseRequest>(),
                    CancellationToken.None))
            .ReturnsAsync(new CcdGetBucket200ResponseLastRelease());

        m_MockClientWrapper.Setup(
                b => b.BadgeClient!.CreateBadgeAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    "",
                    0,
                    CancellationToken.None))
            .ReturnsAsync(
                new CcdGetBucket200ResponseLastReleaseBadgesInner
                {
                    Name = CloudContentDeliveryTestsConstants.BadgeName
                });

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.EnvironmentId);

        await SyncEntryHandler.SyncEntriesAsync(
            cloudContentDeliveryInput,
            m_MockUnityEnvironment.Object,
            m_MockClientWrapper.Object,
            m_MockSynchronizationService.Object,
            m_MockLogger.Object,
            CancellationToken.None);
        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);

    }

    [Test]
    public async Task SyncHandler_ValidInputLogsResultWithDryRunNoCreateRelease()
    {
        var cloudContentDeliveryInput = new CloudContentDeliveryInput
        {
            CloudProjectId = CloudContentDeliveryTestsConstants.ProjectId,
            BucketNameOpt = CloudContentDeliveryTestsConstants.BucketName,
            EntryPath = CloudContentDeliveryTestsConstants.Path,
            VersionId = CloudContentDeliveryTestsConstants.VersionId,
            LocalFolder = CloudContentDeliveryTestsConstants.LocalFolder,
            ExclusionPattern = CloudContentDeliveryTestsConstants.ExclusionPattern,
            Delete = CloudContentDeliveryTestsConstants.Delete,
            Retry = CloudContentDeliveryTestsConstants.Retry,
            DryRun = true,
            UpdateBadge = CloudContentDeliveryTestsConstants.UpdateBadge,
            CreateRelease = false,
            SyncMetadata = CloudContentDeliveryTestsConstants.Metadata,
            ReleaseNotes = CloudContentDeliveryTestsConstants.Notes,
            Labels = CloudContentDeliveryTestsConstants.Labels
        };

        var expectedSyncResult = new SyncResult();
        m_MockSynchronizationService.Setup(
                s => s.CalculateSynchronization(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.LocalFolder,
                    CloudContentDeliveryTestsConstants.ExclusionPattern,
                    CloudContentDeliveryTestsConstants.Delete,
                    CloudContentDeliveryTestsConstants.Labels,
                    CcdUtils.ParseMetadata(CloudContentDeliveryTestsConstants.Metadata),
                    CancellationToken.None))
            .ReturnsAsync(expectedSyncResult);

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.EnvironmentId);

        await SyncEntryHandler.SyncEntriesAsync(
            cloudContentDeliveryInput,
            m_MockUnityEnvironment.Object,
            m_MockClientWrapper.Object,
            m_MockSynchronizationService.Object,
            m_MockLogger.Object,
            CancellationToken.None);
        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);

    }
}
