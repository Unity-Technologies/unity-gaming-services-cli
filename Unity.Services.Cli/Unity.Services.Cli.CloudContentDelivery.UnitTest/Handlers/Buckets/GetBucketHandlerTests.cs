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
public class GetBucketHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<IBucketClient> m_MockBucketClient = new();
    readonly Mock<ILogger> m_MockLogger = new();

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockBucketClient.Reset();
        m_MockLogger.Reset();
        var result = new CcdGetBucket200Response();

        m_MockBucketClient.Setup(
                c =>
                    c.GetBucketAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        CancellationToken.None))
            .ReturnsAsync(result);

    }

    [Test]
    public async Task GetAsync_CallsLoadingIndicatorStartLoading()
    {

        CloudContentDeliveryInput cloudContentDeliveryInput = new()
        {
            CloudProjectId = CloudContentDeliveryTestsConstants.ProjectId,
            BucketName = CloudContentDeliveryTestsConstants.BucketName
        };

        var mockLoadingIndicator = new Mock<ILoadingIndicator>();
        await GetBucketHandler.GetAsync(
            cloudContentDeliveryInput,
            m_MockUnityEnvironment.Object,
            m_MockBucketClient.Object,
            m_MockLogger.Object,
            mockLoadingIndicator.Object,
            CancellationToken.None);
        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task GetHandler_ValidInputLogsResult()
    {

        CloudContentDeliveryInput cloudContentDeliveryInput = new()
        {
            CloudProjectId = CloudContentDeliveryTestsConstants.ProjectId,
            BucketName = CloudContentDeliveryTestsConstants.BucketName
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.EnvironmentId);

        await GetBucketHandler.GetAsync(
            cloudContentDeliveryInput,
            m_MockUnityEnvironment.Object,
            m_MockBucketClient.Object,
            m_MockLogger.Object,
            CancellationToken.None);
        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockBucketClient.Verify(
            api =>
                api.GetBucketAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketName,
                    CancellationToken.None),
            Times.Once);

    }
}
