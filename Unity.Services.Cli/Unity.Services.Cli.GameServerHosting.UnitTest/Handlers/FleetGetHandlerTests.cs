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
class FleetGetHandlerTests : HandlerCommon
{
    [Test]
    public async Task FleetGetAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await FleetGetHandler.FleetGetAsync(
            null!,
            MockUnityEnvironment.Object,
            null!,
            null!,
            mockLoadingIndicator.Object,
            CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(
                It.IsAny<string>(),
                It.IsAny<Func<StatusContext?, Task>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task FleetGetAsync_CallsFetchIdentifierAsync()
    {
        FleetIdInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            FleetId = ValidFleetId
        };

        await FleetGetHandler.FleetGetAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, null)]
    public void FleetGetAsync_NullFleetIdThrowsException(string projectId, string environmentName, string fleetId)
    {
        FleetIdInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            FleetId = fleetId
        };

        Assert.ThrowsAsync<MissingInputException>(
            () =>
                FleetGetHandler.FleetGetAsync(
                    input,
                    MockUnityEnvironment.Object,
                    GameServerHostingService!,
                    MockLogger!.Object,
                    CancellationToken.None
                )
        );

        FleetsApi!.DefaultFleetsClient.Verify(
            api => api.GetFleetAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                0,
                CancellationToken.None
            ),
            Times.Never);

        TestsHelper.VerifyLoggerWasCalled(
            MockLogger!,
            LogLevel.Critical,
            LoggerExtension.ResultEventId,
            Times.Never);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, ValidFleetId)]
    public async Task FleetGetAsync_CallsGetService(string projectId, string environmentName, string fleetId)
    {
        FleetIdInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            FleetId = fleetId
        };

        await FleetGetHandler.FleetGetAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        FleetsApi!.DefaultFleetsClient.Verify(
            api => api.GetFleetAsync(
                new Guid(input.CloudProjectId),
                new Guid(ValidEnvironmentId),
                new Guid(input.FleetId),
                0,
                CancellationToken.None
            ),
            Times.Once);
    }

    [TestCase(InvalidProjectId, InvalidEnvironmentId, InvalidFleetId)]
    [TestCase(ValidProjectId, InvalidEnvironmentId, InvalidFleetId)]
    [TestCase(InvalidProjectId, ValidEnvironmentId, InvalidFleetId)]
    [TestCase(InvalidProjectId, InvalidEnvironmentId, ValidFleetId)]
    public void FleetGetAsync_InvalidInputThrowsException(string projectId, string environmentId, string fleetId)
    {
        FleetIdInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = ValidEnvironmentName,
            FleetId = fleetId
        };
        MockUnityEnvironment.Setup(ex => ex.FetchIdentifierAsync(CancellationToken.None)).ReturnsAsync(environmentId);

        Assert.ThrowsAsync<HttpRequestException>(
            () =>
                FleetGetHandler.FleetGetAsync(
                    input,
                    MockUnityEnvironment.Object,
                    GameServerHostingService!,
                    MockLogger!.Object,
                    CancellationToken.None
                )
        );

        TestsHelper.VerifyLoggerWasCalled(
            MockLogger!,
            LogLevel.Critical,
            LoggerExtension.ResultEventId,
            Times.Never);
    }
}
