using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
class RegionAvailableHandlerTests : HandlerCommon
{
    [Test]
    public async Task RegionAvailableAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await RegionAvailableHandler.RegionAvailableAsync(null!, MockUnityEnvironment.Object, null!, null!,
            mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task BuildListAsync_CallsFetchIdentifierAsync()
    {
        FleetIdInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            FleetId = ValidFleetId
        };

        await RegionAvailableHandler.RegionAvailableAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, ValidFleetId)]
    public async Task RegionAvailableAsync_CallsListService(string projectId, string environmentName, string fleetId)
    {
        FleetIdInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            FleetId = fleetId
        };

        await RegionAvailableHandler.RegionAvailableAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        FleetsApi!.DefaultFleetsClient.Verify(api => api.GetAvailableFleetRegionsAsync(
            new Guid(input.CloudProjectId), new Guid(ValidEnvironmentId),
            new Guid(ValidFleetId), 0, CancellationToken.None
        ), Times.Once);
    }

    [TestCase(InvalidProjectId, InvalidEnvironmentId, ValidFleetId)]
    [TestCase(ValidProjectId, InvalidEnvironmentId, ValidFleetId)]
    [TestCase(InvalidProjectId, ValidEnvironmentId, ValidFleetId)]
    [TestCase(InvalidProjectId, ValidEnvironmentId, ValidFleetId)]
    [TestCase(ValidProjectId, ValidProjectId, InvalidFleetId)]
    public void RegionAvailableAsync_InvalidInputThrowsException(string projectId, string environmentId, string fleetId)
    {
        FleetIdInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = ValidEnvironmentName,
            FleetId = fleetId
        };
        MockUnityEnvironment.Setup(ex => ex.FetchIdentifierAsync(CancellationToken.None)).ReturnsAsync(environmentId);

        Assert.ThrowsAsync<HttpRequestException>(() =>
            RegionAvailableHandler.RegionAvailableAsync(
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
