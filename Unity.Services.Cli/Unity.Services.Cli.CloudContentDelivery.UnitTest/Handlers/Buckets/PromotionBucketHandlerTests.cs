using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.CloudContentDelivery.Handlers.Buckets;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace CloudContentDeliveryTest.Handlers.Buckets;

[TestFixture]
public class PromotionBucketHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<IBucketClient> m_MockBucketClient = new();
    readonly Mock<ILogger> m_MockLogger = new();

    [SetUp]
    public void Setup()
    {
        m_MockUnityEnvironment.Reset();
        m_MockBucketClient.Reset();
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
    public async Task PromotionAsync_CallsLoadingIndicatorStartLoading()
    {
        var input = new CloudContentDeliveryInputBuckets();
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();
        var cancellationToken = CancellationToken.None;

        mockLoadingIndicator.Setup(
                li => li.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()))
            .Returns(Task.CompletedTask);

        await PromotionBucketHandler.PromotionStatusAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockBucketClient.Object,
            m_MockLogger.Object,
            mockLoadingIndicator.Object,
            cancellationToken);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task PromotionHandler_ValidInputLogsResult()
    {
        var cloudContentDeliveryInput = new CloudContentDeliveryInputBuckets
        {
            CloudProjectId = CloudContentDeliveryTestsConstants.ProjectId,
            BucketNameOpt = CloudContentDeliveryTestsConstants.BucketName,
            ReleaseId = CloudContentDeliveryTestsConstants.ReleaseId,
            PromotionId = CloudContentDeliveryTestsConstants.PromotionId
        };
        var cancellationToken = CancellationToken.None;

        m_MockBucketClient.Setup(
                c =>
                    c.GetPromotionAsync(
                        CloudContentDeliveryTestsConstants.ProjectId,
                        CloudContentDeliveryTestsConstants.EnvironmentId,
                        CloudContentDeliveryTestsConstants.BucketId,
                        CloudContentDeliveryTestsConstants.PromotionId,
                        CancellationToken.None))
            .ReturnsAsync(new CcdGetPromotions200ResponseInner());

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.EnvironmentId);

        await PromotionBucketHandler.PromotionStatusAsync(
            cloudContentDeliveryInput,
            m_MockUnityEnvironment.Object,
            m_MockBucketClient.Object,
            m_MockLogger.Object,
            cancellationToken);
        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(cancellationToken), Times.Once);
        m_MockBucketClient.Verify(
            api =>
                api.GetPromotionAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.PromotionId,
                    cancellationToken),
            Times.Once);

    }
}
