using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudContentDelivery.Handlers.Badges;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace CloudContentDeliveryTest.Handlers.Badges;

[TestFixture]
public class CreateBadgeHandlerTests
{
    readonly Mock<IBucketClient> m_MockBucketClient = new();
    [SetUp]
    public void Setup()
    {
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
    public async Task CreateAsync_Success()
    {
        var input = new CloudContentDeliveryInput
        {
            CloudProjectId = CloudContentDeliveryTestsConstants.ProjectId,
            BucketNameOpt = CloudContentDeliveryTestsConstants.BucketName,
            BadgeName = CloudContentDeliveryTestsConstants.BadgeName,
            ReleaseId = CloudContentDeliveryTestsConstants.ReleaseId,
            ReleaseNum = CloudContentDeliveryTestsConstants.ReleaseNumber
        };

        var unityEnvironment = new Mock<IUnityEnvironment>();
        unityEnvironment.Setup(e => e.FetchIdentifierAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.EnvironmentId);



        var badgeClient = new Mock<IBadgeClient>();
        badgeClient.Setup(
                s =>
                    s.CreateBadgeAsync(
                        CloudContentDeliveryTestsConstants.ProjectId,
                        CloudContentDeliveryTestsConstants.EnvironmentId,
                        CloudContentDeliveryTestsConstants.BucketId,
                        CloudContentDeliveryTestsConstants.BadgeName,
                        CloudContentDeliveryTestsConstants.ReleaseNumber,
                        CancellationToken.None))
            .ReturnsAsync(new CcdGetBucket200ResponseLastReleaseBadgesInner());

        var logger = new Mock<ILogger>();

        await CreateBadgeHandler.CreateAsync(
            input,
            unityEnvironment.Object,
            badgeClient.Object,
            m_MockBucketClient.Object,
            logger.Object,
            CancellationToken.None);

        unityEnvironment.Verify(e => e.FetchIdentifierAsync(It.IsAny<CancellationToken>()), Times.Once);
        badgeClient.Verify(
            s =>
                s.CreateBadgeAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.BadgeName,
                    CloudContentDeliveryTestsConstants.ReleaseNumber,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void CreateAsync_Exception()
    {

        var input = new CloudContentDeliveryInput
        {
            CloudProjectId = CloudContentDeliveryTestsConstants.ProjectId,
            BucketNameOpt = CloudContentDeliveryTestsConstants.BucketName,
            BadgeName = CloudContentDeliveryTestsConstants.BadgeName,
            ReleaseId = "",
            ReleaseNum = 0
        };
        var unityEnvironment = new Mock<IUnityEnvironment>();
        unityEnvironment.Setup(e => e.FetchIdentifierAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.EnvironmentId);

        var badgeClient = new Mock<IBadgeClient>();
        badgeClient.Setup(
                s =>
                    s.CreateBadgeAsync(
                        CloudContentDeliveryTestsConstants.ProjectId,
                        CloudContentDeliveryTestsConstants.EnvironmentId,
                        CloudContentDeliveryTestsConstants.BucketId,
                        CloudContentDeliveryTestsConstants.BadgeName,
                        0,
                        It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failed to create badge"));

        var logger = new Mock<ILogger>();

        Assert.ThrowsAsync<Exception>(
            () =>
                CreateBadgeHandler.CreateAsync(
                    input,
                    unityEnvironment.Object,
                    badgeClient.Object,
                    m_MockBucketClient.Object,
                    logger.Object,
                    CancellationToken.None));
    }
}
