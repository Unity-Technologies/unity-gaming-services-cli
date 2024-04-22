using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.CloudContentDelivery.Handlers.Buckets;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace CloudContentDeliveryTest.Handlers.Buckets;

[TestFixture]
public class ListBucketHandlerTests
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
    }

    [Test]
    public async Task ListAsync_CallsLoadingIndicatorStartLoading()
    {
        var input = new CloudContentDeliveryInputBuckets();
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();
        var cancellationToken = CancellationToken.None;

        mockLoadingIndicator.Setup(
                li => li.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()))
            .Returns(Task.CompletedTask);

        await ListBucketHandler.ListAsync(
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
    public async Task ListHandler_ValidInputLogsResult()
    {
        var cloudContentDeliveryInput = new CloudContentDeliveryInputBuckets
        {
            CloudProjectId = CloudContentDeliveryTestsConstants.ProjectId,
            BucketName = CloudContentDeliveryTestsConstants.BucketName,
            Page = CloudContentDeliveryTestsConstants.Page,
            PerPage = CloudContentDeliveryTestsConstants.PerPage,
            FilterName = CloudContentDeliveryTestsConstants.FilterName,
            SortByBucket = CloudContentDeliveryTestsConstants.SortBy,
            SortOrder = CloudContentDeliveryTestsConstants.SortOrder,
            BucketDescription = CloudContentDeliveryTestsConstants.BucketDescription

        };
        var cancellationToken = CancellationToken.None;

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.EnvironmentId);

        m_MockBucketClient.Setup(
                c =>
                    c.ListBucketAsync(
                        CloudContentDeliveryTestsConstants.ProjectId,
                        CloudContentDeliveryTestsConstants.EnvironmentId,
                        CloudContentDeliveryTestsConstants.Page,
                        CloudContentDeliveryTestsConstants.PerPage,
                        CloudContentDeliveryTestsConstants.FilterName,
                        CloudContentDeliveryTestsConstants.BucketDescription,
                        CloudContentDeliveryTestsConstants.SortBy,
                        CloudContentDeliveryTestsConstants.SortOrder,
                        CancellationToken.None))
            .ReturnsAsync(
                new ApiResponse<List<CcdGetBucket200Response>>(
                    HttpStatusCode.OK,
                    new Multimap<string, string>(),
                    new List<CcdGetBucket200Response>()
                ));

        await ListBucketHandler.ListAsync(
            cloudContentDeliveryInput,
            m_MockUnityEnvironment.Object,
            m_MockBucketClient.Object,
            m_MockLogger.Object,
            cancellationToken);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(cancellationToken), Times.Once);
        m_MockBucketClient.Verify(
            api =>
                api.ListBucketAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.Page,
                    CloudContentDeliveryTestsConstants.PerPage,
                    CloudContentDeliveryTestsConstants.FilterName,
                    CloudContentDeliveryTestsConstants.BucketDescription,
                    CloudContentDeliveryTestsConstants.SortBy,
                    CloudContentDeliveryTestsConstants.SortOrder,
                    cancellationToken),
            Times.Once);
    }
}
