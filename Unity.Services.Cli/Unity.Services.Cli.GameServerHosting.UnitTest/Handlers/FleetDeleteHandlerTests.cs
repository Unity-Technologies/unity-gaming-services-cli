using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
class FleetDeleteHandlerTests : HandlerCommon
{
    [Test]
    public async Task FleetDeleteAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await FleetDeleteHandler.FleetDeleteAsync(null!, MockUnityEnvironment.Object, null!, null!,
            mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task FleetDeleteAsync_CallsFetchIdentifierAsync()
    {
        FleetIdInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            FleetId = ValidFleetId
        };

        await FleetDeleteHandler.FleetDeleteAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, null)]
    public void FleetDeleteAsync_NullFleetIdThrowsException(string projectId, string environmentName, string fleetId)
    {
        FleetIdInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            FleetId = fleetId
        };

        Assert.ThrowsAsync<MissingInputException>(() =>
            FleetDeleteHandler.FleetDeleteAsync(
                input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                CancellationToken.None
            )
        );

        FleetsApi!.DefaultFleetsClient.Verify(api => api.GetFleetAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<Guid>(), 0, CancellationToken.None
        ), Times.Never);

        TestsHelper.VerifyLoggerWasCalled(MockLogger!, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Never);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, ValidFleetId)]
    public async Task FleetDeleteAsync_CallsDeleteService(string projectId, string environmentName, string fleetId)
    {
        FleetIdInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            FleetId = fleetId
        };

        await FleetDeleteHandler.FleetDeleteAsync(input, MockUnityEnvironment.Object, GameServerHostingService!,
            MockLogger!.Object, CancellationToken.None);

        FleetsApi!.DefaultFleetsClient.Verify(api => api.DeleteFleetAsync(
            new Guid(input.CloudProjectId), new Guid(ValidEnvironmentId),
            new Guid(input.FleetId), null, 0, CancellationToken.None
        ), Times.Once);

        // Clear invocations to Mock Environment
        MockUnityEnvironment.Invocations.Clear();
    }

    [TestCase(InvalidProjectId, InvalidEnvironmentId, InvalidFleetId)]
    [TestCase(ValidProjectId, InvalidEnvironmentId, InvalidFleetId)]
    [TestCase(InvalidProjectId, ValidEnvironmentId, InvalidFleetId)]
    [TestCase(InvalidProjectId, InvalidEnvironmentId, InvalidFleetId)]
    public void FleetDeleteAsync_InvalidInputThrowsException(string projectId, string environmentId, string fleetId)
    {
        FleetIdInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = ValidEnvironmentName,
            FleetId = fleetId
        };
        MockUnityEnvironment.Setup(ex => ex.FetchIdentifierAsync(CancellationToken.None)).ReturnsAsync(environmentId);

        Assert.ThrowsAsync<HttpRequestException>(() =>
            FleetDeleteHandler.FleetDeleteAsync(
                input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                CancellationToken.None
            )
        );

        TestsHelper.VerifyLoggerWasCalled(MockLogger!, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Never);
    }
}
