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
using Action = Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model.CcdUpdatePermissionByBucketRequest.ActionEnum;
using Permission = Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model.CcdUpdatePermissionByBucketRequest.PermissionEnum;
using Role = Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model.CcdUpdatePermissionByBucketRequest.RoleEnum;

namespace CloudContentDeliveryTest.Handlers.Buckets;

[TestFixture]
public class PermissionBucketHandlerTests
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

        var result = new CcdGetAllByBucket200ResponseInner();

        m_MockBucketClient.Setup(
                c =>
                    c.UpdatePermissionBucketAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Action>(),
                        It.IsAny<Permission>(),
                        It.IsAny<Role>(),
                        CancellationToken.None))
            .ReturnsAsync(result);

    }

    [Test]
    public async Task PermissionUpdateAsync_CallsLoadingIndicatorStartLoading()
    {
        var input = new CloudContentDeliveryInputBuckets();
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();
        var cancellationToken = CancellationToken.None;

        mockLoadingIndicator.Setup(
                li => li.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()))
            .Returns(Task.CompletedTask);

        await PermissionBucketHandler.PermissionUpdateAsync(
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
    [TestCase(Action.Write, Permission.Allow, Role.User)]
    [TestCase(Action.Write, Permission.Allow, Role.Client)]
    [TestCase(Action.Write, Permission.Deny, Role.User)]
    [TestCase(Action.Write, Permission.Deny, Role.Client)]
    [TestCase(Action.ListEntries, Permission.Allow, Role.User)]
    [TestCase(Action.ListEntries, Permission.Allow, Role.Client)]
    public async Task PermissionHandler_ValidInputLogsResult(Action action, Permission permission, Role role)
    {
        var cloudContentDeliveryInput = new CloudContentDeliveryInputBuckets
        {
            CloudProjectId = CloudContentDeliveryTestsConstants.ProjectId,
            BucketName = CloudContentDeliveryTestsConstants.BucketName,
            Action = action,
            Permission = permission,
            Role = role
        };
        var cancellationToken = CancellationToken.None;

        m_MockUnityEnvironment
            .Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(CloudContentDeliveryTestsConstants.EnvironmentId);

        await PermissionBucketHandler.PermissionUpdateAsync(
            cloudContentDeliveryInput,
            m_MockUnityEnvironment.Object,
            m_MockBucketClient.Object,
            m_MockLogger.Object,
            cancellationToken);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(cancellationToken), Times.Once);
        m_MockBucketClient.Verify(
            api =>
                api.UpdatePermissionBucketAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketName,
                    action,
                    permission,
                    role,
                    cancellationToken),
            Times.Once);
    }
}
