using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
class FleetUpdateHandlerTests : HandlerCommon
{
    [Test]
    public async Task FleetUpdateAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await FleetUpdateHandler.FleetUpdateAsync(null!, MockUnityEnvironment.Object, null!, null!,
            mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task FleetUpdateAsync_CallsFetchIdentifierAsync()
    {
        FleetUpdateInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            FleetId = ValidFleetId,
            AllocTtl = 0,
            BuildConfigs = new List<long>(),
            DisabledDeleteTtl = 0,
            ShutdownTtl = 0,
            DeleteTtl = 0
        };

        await FleetUpdateHandler.FleetUpdateAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, null)]
    public void FleetUpdateAsync_NullFleetIdThrowsException(
        string projectId,
        string environmentName,
        string fleetId
    )
    {
        FleetUpdateInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            FleetId = fleetId
        };

        Assert.ThrowsAsync<MissingInputException>(() =>
            FleetUpdateHandler.FleetUpdateAsync(
                input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                CancellationToken.None
            )
        );

        FleetsApi!.DefaultFleetsClient.Verify(api => api.UpdateFleetAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<Guid>(), null, 0, CancellationToken.None
        ), Times.Never);

        TestsHelper.VerifyLoggerWasCalled(MockLogger!, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Never);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, ValidFleetId)]
    public async Task FleetUpdateAsync_CallsUpdateService(
        string projectId,
        string environmentName,
        string fleetId
    )
    {
        FleetUpdateInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            FleetId = ValidFleetId,
            Name = ValidFleetName,
            AllocTtl = 0,
            DeleteTtl = 0,
            BuildConfigs = new List<long>() { 1 },
            DisabledDeleteTtl = 0,
            ShutdownTtl = 0
        };

        await FleetUpdateHandler.FleetUpdateAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        FleetUpdateRequest req = new FleetUpdateRequest(name: input.Name, buildConfigurations: input.BuildConfigs);

        FleetsApi!.DefaultFleetsClient.Verify(api => api.UpdateFleetAsync(
            new Guid(input.CloudProjectId), new Guid(ValidEnvironmentId),
            new Guid(fleetId), req, 0, CancellationToken.None
        ), Times.Once);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, ValidFleetId)]
    public async Task FleetUpdateAsync_CallsGetFleetIfNoNameOrBuildConfigs(
        string projectId,
        string environmentName,
        string fleetId
    )
    {
        FleetUpdateInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            FleetId = ValidFleetId,
            Name = ValidFleetName,
            AllocTtl = 0,
            DeleteTtl = 0,
            BuildConfigs = new List<long>() { 1 },
            DisabledDeleteTtl = 0,
            ShutdownTtl = 0
        };

        await FleetUpdateHandler.FleetUpdateAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        FleetUpdateRequest expected = new FleetUpdateRequest(name: input.Name, buildConfigurations: input.BuildConfigs);

        FleetsApi!.DefaultFleetsClient.Verify(api => api.UpdateFleetAsync(
            new Guid(input.CloudProjectId), new Guid(ValidEnvironmentId),
            new Guid(fleetId), expected, 0, CancellationToken.None
        ), Times.Once);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, InvalidFleetId)]
    public void FleetUpdateAsync_InvalidFleetIdThrowsException(
        string projectId,
        string environmentName,
        string fleetId
    )
    {
        FleetUpdateInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            FleetId = fleetId,
            Name = ValidFleetName,
            AllocTtl = 0,
            DeleteTtl = 0,
            BuildConfigs = new List<long>(),
            DisabledDeleteTtl = 0,
            ShutdownTtl = 0
        };

        MockUnityEnvironment.Setup(ex => ex.FetchIdentifierAsync(CancellationToken.None)).ReturnsAsync(ValidEnvironmentId);

        Assert.ThrowsAsync<HttpRequestException>(() =>
            FleetUpdateHandler.FleetUpdateAsync(
                input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                CancellationToken.None
            )
        );

        TestsHelper.VerifyLoggerWasCalled(MockLogger!, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Never);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, ValidFleetId)]
    public async Task FleetUpdateAsync_FillNameAndBuildConfigsIfNotProvided(
        string projectId,
        string environmentName,
        string fleetId
    )
    {
        FleetUpdateInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            FleetId = fleetId,
            Name = null,
            AllocTtl = 0,
            DeleteTtl = 0,
            BuildConfigs = new List<long>(),
            DisabledDeleteTtl = 0,
            ShutdownTtl = 0
        };

        await FleetUpdateHandler.FleetUpdateAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        FleetUpdateRequest expected =
            new FleetUpdateRequest(name: ValidFleetName, buildConfigurations: new List<long>() { 1 });

        FleetsApi!.DefaultFleetsClient.Verify(api => api.UpdateFleetAsync(
            new Guid(input.CloudProjectId), new Guid(ValidEnvironmentId),
            new Guid(fleetId), expected, 0, CancellationToken.None
        ), Times.Once);
    }
}
