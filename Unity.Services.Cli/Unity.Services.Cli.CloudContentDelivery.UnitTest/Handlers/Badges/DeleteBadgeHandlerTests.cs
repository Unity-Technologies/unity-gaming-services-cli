using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.CloudContentDelivery.Handlers.Badges;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;

namespace CloudContentDeliveryTest.Handlers.Badges;

[TestFixture]
public class DeleteBadgeHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<IBadgeClient> m_MockBadgeClient = new();
    readonly Mock<IBucketClient> m_MockBucketClient = new();
    readonly Mock<ILogger> m_MockLogger = new();

    [SetUp]
    public void Setup()
    {
        m_MockUnityEnvironment.Reset();
        m_MockBadgeClient.Reset();
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
    public async Task DeleteAsync_CallsLoadingIndicatorStartLoading()
    {
        var input = new CloudContentDeliveryInput();
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();
        var cancellationToken = CancellationToken.None;

        mockLoadingIndicator.Setup(
                li => li.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()))
            .Returns(Task.CompletedTask);

        await DeleteBadgeHandler.DeleteAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockBadgeClient.Object,
            m_MockBucketClient.Object,
            m_MockLogger.Object,
            mockLoadingIndicator.Object,
            cancellationToken);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task DeleteHandler_ValidInputLogsResult()
    {
        var cloudContentDeliveryInput = new CloudContentDeliveryInput
        {
            CloudProjectId = CloudContentDeliveryTestsConstants.ProjectId,
            BucketNameOpt = CloudContentDeliveryTestsConstants.BucketName,
            BadgeName = CloudContentDeliveryTestsConstants.BadgeName
        };
        var cancellationToken = CancellationToken.None;

        m_MockBadgeClient.Setup(
                c =>
                    c.DeleteBadgeAsync(
                        CloudContentDeliveryTestsConstants.ProjectId,
                        CloudContentDeliveryTestsConstants.EnvironmentId,
                        CloudContentDeliveryTestsConstants.BucketId,
                        CloudContentDeliveryTestsConstants.BadgeName,
                        CancellationToken.None))
            .ReturnsAsync("Badge deleted.");

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.EnvironmentId);

        await DeleteBadgeHandler.DeleteAsync(
            cloudContentDeliveryInput,
            m_MockUnityEnvironment.Object,
            m_MockBadgeClient.Object,
            m_MockBucketClient.Object,
            m_MockLogger.Object,
            cancellationToken);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(cancellationToken), Times.Once);
        m_MockBadgeClient.Verify(
            api =>
                api.DeleteBadgeAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.BadgeName,
                    cancellationToken),
            Times.Once);

    }
}
