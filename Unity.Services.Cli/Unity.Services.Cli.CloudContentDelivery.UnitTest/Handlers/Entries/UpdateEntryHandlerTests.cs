using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.CloudContentDelivery.Handlers.Entries;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace CloudContentDeliveryTest.Handlers.Entries;

[TestFixture]
public class UpdateEntryHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<IEntryClient> m_MockEntryClient = new();
    readonly Mock<IBucketClient> m_MockBucketClient = new();
    readonly Mock<ILogger> m_MockLogger = new();

    [SetUp]
    public void Setup()
    {
        m_MockUnityEnvironment.Reset();
        m_MockEntryClient.Reset();
        m_MockLogger.Reset();

        m_MockBucketClient.Setup(
                c =>
                    c.GetBucketIdByName(
                        CloudContentDeliveryTestsConstants.ProjectId,
                        CloudContentDeliveryTestsConstants.EnvironmentId,
                        CloudContentDeliveryTestsConstants.BucketName,
                        CancellationToken.None))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.BucketId);
    }

    [Test]
    public async Task UpdateEntryAsync_CallsLoadingIndicatorStartLoading()
    {
        var input = new CloudContentDeliveryInput();
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();
        var cancellationToken = CancellationToken.None;

        mockLoadingIndicator.Setup(
                li => li.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()))
            .Returns(Task.CompletedTask);
        await UpdateEntryHandler.UpdateAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockEntryClient.Object,
            m_MockBucketClient.Object,
            m_MockLogger.Object,
            mockLoadingIndicator.Object,
            cancellationToken);
        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task UpdateHandler_ValidInputLogsResult()
    {
        var cloudContentDeliveryInput = new CloudContentDeliveryInput
        {
            CloudProjectId = CloudContentDeliveryTestsConstants.ProjectId,
            BucketNameOpt = CloudContentDeliveryTestsConstants.BucketName,
            EntryPath = CloudContentDeliveryTestsConstants.Path,
            VersionId = CloudContentDeliveryTestsConstants.VersionId,
            Labels = CloudContentDeliveryTestsConstants.Labels,
            Metadata = CloudContentDeliveryTestsConstants.Metadata

        };
        var cancellationToken = CancellationToken.None;

        m_MockEntryClient.Setup(
                c =>
                    c.UpdateEntryAsync(
                        CloudContentDeliveryTestsConstants.ProjectId,
                        CloudContentDeliveryTestsConstants.EnvironmentId,
                        CloudContentDeliveryTestsConstants.BucketId,
                        CloudContentDeliveryTestsConstants.Path,
                        CloudContentDeliveryTestsConstants.VersionId,
                        CloudContentDeliveryTestsConstants.Labels,
                        CloudContentDeliveryTestsConstants.Metadata,
                        CancellationToken.None))
            .ReturnsAsync(new CcdCreateOrUpdateEntryBatch200ResponseInner());

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.EnvironmentId);

        await UpdateEntryHandler.UpdateAsync(
            cloudContentDeliveryInput,
            m_MockUnityEnvironment.Object,
            m_MockEntryClient.Object,
            m_MockBucketClient.Object,
            m_MockLogger.Object,
            cancellationToken);
        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(cancellationToken), Times.Once);
        m_MockEntryClient.Verify(
            api =>
                api.UpdateEntryAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.Path,
                    CloudContentDeliveryTestsConstants.VersionId,
                    CloudContentDeliveryTestsConstants.Labels,
                    CloudContentDeliveryTestsConstants.Metadata,
                    cancellationToken),
            Times.Once);

    }
}
