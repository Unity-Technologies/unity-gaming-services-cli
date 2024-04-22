using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudContentDelivery.Handlers.Buckets;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace CloudContentDeliveryTest.Handlers.Buckets;

[TestFixture]
public class CreateBucketHandlerTests
{
    [Test]
    public async Task CreateAsync_Success()
    {
        var input = new CloudContentDeliveryInputBuckets
        {
            CloudProjectId = CloudContentDeliveryTestsConstants.ProjectId,
            BucketName = CloudContentDeliveryTestsConstants.BucketName,
            BucketDescription = CloudContentDeliveryTestsConstants.BucketDescription,
            BucketPrivate = true
        };

        var unityEnvironment = new Mock<IUnityEnvironment>();
        unityEnvironment.Setup(e => e.FetchIdentifierAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.EnvironmentId);

        var bucketClient = new Mock<IBucketClient>();
        bucketClient.Setup(
                s =>
                    s.CreateBucketAsync(
                        CloudContentDeliveryTestsConstants.ProjectId,
                        CloudContentDeliveryTestsConstants.EnvironmentId,
                        CloudContentDeliveryTestsConstants.BucketName,
                        CloudContentDeliveryTestsConstants.BucketDescription,
                        true,
                        It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CcdGetBucket200Response());

        var logger = new Mock<ILogger>();
        await CreateBucketHandler.CreateAsync(
            input,
            unityEnvironment.Object,
            bucketClient.Object,
            logger.Object,
            CancellationToken.None);
        unityEnvironment.Verify(e => e.FetchIdentifierAsync(It.IsAny<CancellationToken>()), Times.Once);
        bucketClient.Verify(
            s =>
                s.CreateBucketAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketName,
                    CloudContentDeliveryTestsConstants.BucketDescription,
                    true,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void CreateAsync_Exception()
    {

        var input = new CloudContentDeliveryInputBuckets
        {
            CloudProjectId = CloudContentDeliveryTestsConstants.ProjectId,
            BucketName = CloudContentDeliveryTestsConstants.BucketName,
            BucketDescription = CloudContentDeliveryTestsConstants.BucketDescription,
            BucketPrivate = true
        };
        var unityEnvironment = new Mock<IUnityEnvironment>();
        unityEnvironment.Setup(e => e.FetchIdentifierAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.EnvironmentId);

        var bucketClient = new Mock<IBucketClient>();
        bucketClient.Setup(
                s =>
                    s.CreateBucketAsync(
                        CloudContentDeliveryTestsConstants.ProjectId,
                        CloudContentDeliveryTestsConstants.EnvironmentId,
                        CloudContentDeliveryTestsConstants.BucketName,
                        CloudContentDeliveryTestsConstants.BucketDescription,
                        true,
                        It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failed to create bucket"));

        var logger = new Mock<ILogger>();

        Assert.ThrowsAsync<Exception>(
            () =>
                CreateBucketHandler.CreateAsync(
                    input,
                    unityEnvironment.Object,
                    bucketClient.Object,
                    logger.Object,
                    CancellationToken.None));
    }
}
