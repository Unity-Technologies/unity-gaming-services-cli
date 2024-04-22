using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.CloudContentDelivery.Handlers.Entries;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace CloudContentDeliveryTest.Handlers.Entries;

[TestFixture]
public class ListEntryHandlerTests
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
    public async Task ListAsync_CallsLoadingIndicatorStartLoading()
    {
        var input = new CloudContentDeliveryInput();
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();
        var cancellationToken = CancellationToken.None;

        mockLoadingIndicator.Setup(
                li => li.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()))
            .Returns(Task.CompletedTask);

        await ListEntryHandler.ListAsync(
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
    public async Task ListHandler_ValidInputLogsResult()
    {
        var cloudContentDeliveryInput = new CloudContentDeliveryInput
        {
            CloudProjectId = CloudContentDeliveryTestsConstants.ProjectId,
            BucketNameOpt = CloudContentDeliveryTestsConstants.BucketName,
            Page = CloudContentDeliveryTestsConstants.Page,
            PerPage = CloudContentDeliveryTestsConstants.PerPage,
            StartingAfter = CloudContentDeliveryTestsConstants.StartingAfter,
            Path = CloudContentDeliveryTestsConstants.Path,
            Label = CloudContentDeliveryTestsConstants.Label,
            ContentType = CloudContentDeliveryTestsConstants.ContentType,
            Complete = CloudContentDeliveryTestsConstants.Complete,
            SortByEntry = CloudContentDeliveryTestsConstants.SortBy,
            SortOrder = CloudContentDeliveryTestsConstants.SortOrder

        };
        var cancellationToken = CancellationToken.None;

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.EnvironmentId);

        m_MockEntryClient.Setup(
                c =>
                    c.ListEntryAsync(
                        CloudContentDeliveryTestsConstants.ProjectId,
                        CloudContentDeliveryTestsConstants.EnvironmentId,
                        CloudContentDeliveryTestsConstants.BucketId,
                        CloudContentDeliveryTestsConstants.Page,
                        CloudContentDeliveryTestsConstants.StartingAfter,
                        CloudContentDeliveryTestsConstants.PerPage,
                        CloudContentDeliveryTestsConstants.Path,
                        CloudContentDeliveryTestsConstants.Label,
                        CloudContentDeliveryTestsConstants.ContentType,
                        CloudContentDeliveryTestsConstants.Complete,
                        CloudContentDeliveryTestsConstants.SortBy,
                        CloudContentDeliveryTestsConstants.SortOrder,
                        CancellationToken.None))
            .ReturnsAsync(
                new ApiResponse<List<CcdCreateOrUpdateEntryBatch200ResponseInner>>(
                    HttpStatusCode.OK,
                    new Multimap<string, string>(),
                    new List<CcdCreateOrUpdateEntryBatch200ResponseInner>()
                ));

        await ListEntryHandler.ListAsync(
            cloudContentDeliveryInput,
            m_MockUnityEnvironment.Object,
            m_MockEntryClient.Object,
            m_MockBucketClient.Object,
            m_MockLogger.Object,
            cancellationToken);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(cancellationToken), Times.Once);
        m_MockEntryClient.Verify(
            api =>
                api.ListEntryAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.Page,
                    CloudContentDeliveryTestsConstants.StartingAfter,
                    CloudContentDeliveryTestsConstants.PerPage,
                    CloudContentDeliveryTestsConstants.Path,
                    CloudContentDeliveryTestsConstants.Label,
                    CloudContentDeliveryTestsConstants.ContentType,
                    CloudContentDeliveryTestsConstants.Complete,
                    CloudContentDeliveryTestsConstants.SortBy,
                    CloudContentDeliveryTestsConstants.SortOrder,
                    cancellationToken),
            Times.Once);
    }
}
