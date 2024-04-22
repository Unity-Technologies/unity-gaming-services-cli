using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.CloudContentDelivery.Handlers.Badges;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace CloudContentDeliveryTest.Handlers.Badges;

[TestFixture]
public class ListBadgeHandlerTests
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
    public async Task ListAsync_CallsLoadingIndicatorStartLoading()
    {
        var input = new CloudContentDeliveryInput();
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();
        var cancellationToken = CancellationToken.None;

        mockLoadingIndicator.Setup(
                li => li.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()))
            .Returns(Task.CompletedTask);

        await ListBadgeHandler.ListAsync(
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
    public async Task ListHandler_ValidInputLogsResult()
    {
        var cloudContentDeliveryInput = new CloudContentDeliveryInput
        {
            CloudProjectId = CloudContentDeliveryTestsConstants.ProjectId,
            BucketNameOpt = CloudContentDeliveryTestsConstants.BucketName,
            Page = CloudContentDeliveryTestsConstants.Page,
            PerPage = CloudContentDeliveryTestsConstants.PerPage,
            ReleaseNumOption = CloudContentDeliveryTestsConstants.ReleaseNumber.ToString(),
            FilterName = CloudContentDeliveryTestsConstants.FilterName,
            SortByBadge = CloudContentDeliveryTestsConstants.SortBy,
            SortOrder = CloudContentDeliveryTestsConstants.SortOrder
        };

        var cancellationToken = CancellationToken.None;
        var environmentId = CloudContentDeliveryTestsConstants.EnvironmentId;

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(environmentId);

        ApiResponse<List<CcdGetBucket200ResponseLastReleaseBadgesInner>> badgeListResult = new(
            HttpStatusCode.OK,
            new Multimap<string, string>(),
            new List<CcdGetBucket200ResponseLastReleaseBadgesInner>());

        m_MockBadgeClient.Setup(
                api => api.ListBadgeAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.Page,
                    CloudContentDeliveryTestsConstants.PerPage,
                    CloudContentDeliveryTestsConstants.FilterName,
                    CloudContentDeliveryTestsConstants.ReleaseNumber.ToString(),
                    CloudContentDeliveryTestsConstants.SortBy,
                    CloudContentDeliveryTestsConstants.SortOrder,
                    cancellationToken))
            .ReturnsAsync(badgeListResult);

        await ListBadgeHandler.ListAsync(
            cloudContentDeliveryInput,
            m_MockUnityEnvironment.Object,
            m_MockBadgeClient.Object,
            m_MockBucketClient.Object,
            m_MockLogger.Object,
            cancellationToken);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(cancellationToken), Times.Once);
        m_MockBadgeClient.Verify(
            api => api.ListBadgeAsync(
                CloudContentDeliveryTestsConstants.ProjectId,
                CloudContentDeliveryTestsConstants.EnvironmentId,
                CloudContentDeliveryTestsConstants.BucketId,
                CloudContentDeliveryTestsConstants.Page,
                CloudContentDeliveryTestsConstants.PerPage,
                CloudContentDeliveryTestsConstants.FilterName,
                CloudContentDeliveryTestsConstants.ReleaseNumber.ToString(),
                CloudContentDeliveryTestsConstants.SortBy,
                CloudContentDeliveryTestsConstants.SortOrder,
                cancellationToken),
            Times.Once);

    }
}
