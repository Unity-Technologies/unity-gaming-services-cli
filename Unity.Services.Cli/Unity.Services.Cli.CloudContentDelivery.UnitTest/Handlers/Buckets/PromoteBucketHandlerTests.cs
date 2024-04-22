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
public class PromoteBucketHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<IBucketClient> m_MockBucketClient = new();
    readonly Mock<IReleaseClient> m_MockReleaseClient = new();
    readonly Mock<ILogger> m_MockLogger = new();

    [SetUp]
    public void Setup()
    {
        m_MockUnityEnvironment.Reset();
        m_MockBucketClient.Reset();
        m_MockLogger.Reset();


        m_MockUnityEnvironment
            .Setup(
                ue => ue.FetchIdentifierFromSpecificEnvironmentNameAsync(
                    CloudContentDeliveryTestsConstants.ToEnvironment,
                    CancellationToken.None))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.EnvironmentId);
    }

    [Test]
    public async Task PromoteAsync_CallsLoadingIndicatorStartLoading()
    {
        var input = new CloudContentDeliveryInputBuckets();
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();
        var cancellationToken = CancellationToken.None;

        mockLoadingIndicator.Setup(
                li => li.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()))
            .Returns(Task.CompletedTask);
        await PromoteBucketHandler.PromoteAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockReleaseClient.Object,
            m_MockBucketClient.Object,
            m_MockLogger.Object,
            mockLoadingIndicator.Object,
            cancellationToken);
        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task PromoteHandler_ValidInputLogsResult()
    {
        var cloudContentDeliveryInput = new CloudContentDeliveryInputBuckets
        {
            CloudProjectId = CloudContentDeliveryTestsConstants.ProjectId,
            BucketNameOpt = CloudContentDeliveryTestsConstants.BucketName,
            ReleaseNum = CloudContentDeliveryTestsConstants.ReleaseNumber,
            Notes = CloudContentDeliveryTestsConstants.Notes,
            TargetBucketName = CloudContentDeliveryTestsConstants.TargetBucketName,
            TargetEnvironment = CloudContentDeliveryTestsConstants.ToEnvironment
        };
        var cancellationToken = CancellationToken.None;

        m_MockBucketClient.Setup(
                c =>
                    c.GetBucketIdByName(
                        CloudContentDeliveryTestsConstants.ProjectId,
                        CloudContentDeliveryTestsConstants.EnvironmentId,
                        CloudContentDeliveryTestsConstants.BucketName,
                        CancellationToken.None))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.BucketId);

        m_MockBucketClient.Setup(
                c =>
                    c.GetBucketIdByName(
                        CloudContentDeliveryTestsConstants.ProjectId,
                        CloudContentDeliveryTestsConstants.EnvironmentId,
                        CloudContentDeliveryTestsConstants.TargetBucketName,
                        CancellationToken.None))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.ToBucket);

        m_MockReleaseClient.Setup(
                c =>
                    c.GetReleaseIdByNumber(
                        CloudContentDeliveryTestsConstants.ProjectId,
                        CloudContentDeliveryTestsConstants.EnvironmentId,
                        CloudContentDeliveryTestsConstants.BucketId,
                        CloudContentDeliveryTestsConstants.ReleaseNumber,
                        CancellationToken.None))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.PromotedFromRelease);

        m_MockBucketClient.Setup(
                c =>
                    c.PromoteBucketEnvAsync(
                        CloudContentDeliveryTestsConstants.ProjectId,
                        CloudContentDeliveryTestsConstants.EnvironmentId,
                        CloudContentDeliveryTestsConstants.BucketId,
                        CloudContentDeliveryTestsConstants.PromotedFromRelease,
                        CloudContentDeliveryTestsConstants.Notes,
                        CloudContentDeliveryTestsConstants.ToBucket,
                        CloudContentDeliveryTestsConstants.EnvironmentId,
                        CancellationToken.None))
            .ReturnsAsync(new CcdPromoteBucketAsync200Response());

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.EnvironmentId);

        await PromoteBucketHandler.PromoteAsync(
            cloudContentDeliveryInput,
            m_MockUnityEnvironment.Object,
            m_MockReleaseClient.Object,
            m_MockBucketClient.Object,
            m_MockLogger.Object,
            cancellationToken);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(cancellationToken), Times.Once);
        m_MockBucketClient.Verify(
            api =>
                api.PromoteBucketEnvAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.PromotedFromRelease,
                    CloudContentDeliveryTestsConstants.Notes,
                    CloudContentDeliveryTestsConstants.ToBucket,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    cancellationToken),
            Times.Once);

    }
}
