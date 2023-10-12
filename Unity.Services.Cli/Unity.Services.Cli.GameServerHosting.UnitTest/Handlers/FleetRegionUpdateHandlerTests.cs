using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
class FleetRegionUpdateHandlerTests : HandlerCommon
{
    [Test]
    public async Task FleetRegionUpdateAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await FleetRegionUpdateHandler.FleetRegionUpdateAsync(
            null!,
            MockUnityEnvironment.Object,
            null!,
            null!,
            mockLoadingIndicator.Object,
            CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task FleetRegionUpdateAsync_CallsFetchIdentifierAsync()
    {
        FleetRegionUpdateInput input = new()
        {
            CloudProjectId = ValidProjectId,
            FleetId = Guid.Parse(ValidFleetId),
            RegionId = Guid.Parse(ValidRegionId),
            DeleteTtl = 120,
            DisabledDeleteTtl = 60,
            MaxServers = 3,
            MinAvailableServers = 3,
            ScalingEnabled = false.ToString(),
            ShutdownTtl = 180
        };

        await FleetRegionUpdateHandler.FleetRegionUpdateAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(ValidProjectId, ValidFleetId, ValidRegionId, 120, 60, 3, 3, "false", 180, false, TestName = "Golden path")]
    [TestCase(ValidProjectId, ValidFleetId, ValidRegionId, 120, 60, 3, 3, null, 180, true, TestName = "No Scaling Enabled supplied, maxServers and minAvailableServers both > 0")]
    [TestCase(ValidProjectId, ValidFleetId, ValidRegionId, 120, 60, 3, 0, null, 180, true, TestName = "No Scaling Enabled supplied, maxServers > 0 minAvailableServers = 0")]
    [TestCase(ValidProjectId, ValidFleetId, ValidRegionId, 120, 60, 0, 3, null, 180, true, TestName = "No Scaling Enabled supplied, maxServers = 0 minAvailableServers > 0")]
    [TestCase(ValidProjectId, ValidFleetId, ValidRegionId, 120, 60, 0, 0, null, 180, false, TestName = "No Scaling Enabled supplied, maxServers and minAvailableServers both = 0")]
    public async Task FleetRegionUpdateAsync_CallsUpdateService(string projectId, string fleetId, string regionId, long deleteTtl,
        long disabledDeleteTtl, long maxServers, long minAvailableServers, string scalingEnabled, long shutdownTtl, bool defaultScalingEnabled)
    {
        FleetRegionUpdateInput input = new()
        {
            CloudProjectId = projectId,
            FleetId = Guid.Parse(fleetId),
            RegionId = Guid.Parse(regionId),
            DeleteTtl = deleteTtl,
            DisabledDeleteTtl = disabledDeleteTtl,
            MaxServers = maxServers,
            MinAvailableServers = minAvailableServers,
            ScalingEnabled = scalingEnabled,
            ShutdownTtl = shutdownTtl
        };

        await FleetRegionUpdateHandler.FleetRegionUpdateAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        var updateRegionRequest = new UpdateRegionRequest(
            deleteTtl,
            disabledDeleteTtl,
            maxServers,
            minAvailableServers,
            defaultScalingEnabled,
            shutdownTtl
        );

        FleetsApi!.DefaultFleetsClient.Verify(api => api.UpdateFleetRegionAsync(
            new Guid(projectId), new Guid(ValidEnvironmentId),
            new Guid(fleetId), new Guid(regionId), updateRegionRequest, 0, CancellationToken.None
            ), Times.Once);
    }

    [TestCase(null, ValidFleetId, ValidRegionId, 120, 60, 3, 3, "false", 180, typeof(ArgumentNullException), TestName = "Null Project Id throws ArgumentNullException")]
    [TestCase(InvalidProjectId, ValidFleetId, ValidRegionId, 120, 60, 3, 3, "false", 180, typeof(HttpRequestException), TestName = "Invalid Project Id throws HttpRequestException")]
    [TestCase(ValidProjectId, null, ValidRegionId, 120, 60, 3, 3, "false", 180, typeof(MissingInputException), TestName = "Null Fleet Id throws ArgumentNullException")]
    [TestCase(ValidProjectId, InvalidFleetId, ValidRegionId, 120, 60, 3, 3, "false", 180, typeof(HttpRequestException), TestName = "Invalid Fleet Id throws HttpRequestException")]
    [TestCase(ValidProjectId, ValidFleetId, null, 120, 60, 3, 3, "false", 180, typeof(MissingInputException), TestName = "Null Region Id throws MissingInputException")]
    [TestCase(ValidProjectId, ValidFleetId, InvalidRegionId, 120, 60, 3, 3, "false", 180, typeof(CliException), TestName = "Invalid Region Id throws HttpRequestException")]
    [TestCase(ValidProjectId, ValidFleetId, InvalidRegionId, 120, 60, null, 3, "false", 180, typeof(CliException), TestName = "Null Max Servers throws MissingInputException")]
    [TestCase(ValidProjectId, ValidFleetId, InvalidRegionId, 120, 60, 3, null, "false", 180, typeof(CliException), TestName = "Null Min Available Servers throws MissingInputException")]
    [TestCase(ValidProjectId, ValidFleetId, InvalidRegionId, 120, 60, 3, 3, null, 180, typeof(CliException), TestName = "Null Scaling Enabled throws MissingInputException")]
    [TestCase(ValidProjectId, ValidFleetId, InvalidRegionId, null, 60, 3, 3, "false", 180, typeof(MissingInputException), TestName = "Null Delete Ttl throws MissingInputException")]
    [TestCase(ValidProjectId, ValidFleetId, InvalidRegionId, 120, null, 3, 3, null, 180, typeof(MissingInputException), TestName = "Null Disabled Delete Ttl throws MissingInputException")]
    [TestCase(ValidProjectId, ValidFleetId, InvalidRegionId, 120, 60, 3, 3, "false", null, typeof(MissingInputException), TestName = "Null Shutdown Ttl throws MissingInputException")]
    [TestCase(ValidProjectId, ValidFleetId, ValidRegionId, 120, 60, 3, 3, "flse", 180, typeof(CliException), TestName = "Mis-formed bool throws CliException")]
    public Task FleetRegionUpdateAsync_InvalidInputThrowsException(string? projectId, string? fleetId, string? regionId,
        long? deleteTtl, long? disabledDeleteTtl, long? maxServers, long? minAvailableServers, string? scalingEnabled,
        long? shutdownTtl, Type exceptionType)
    {
        FleetRegionUpdateInput input = new()
        {
            CloudProjectId = projectId,
            FleetId = fleetId == null ? null : new Guid(fleetId),
            RegionId = regionId == null ? null : new Guid(regionId),
            DeleteTtl = deleteTtl,
            DisabledDeleteTtl = disabledDeleteTtl,
            MaxServers = maxServers,
            MinAvailableServers = minAvailableServers,
            ScalingEnabled = scalingEnabled,
            ShutdownTtl = shutdownTtl
        };

        Assert.ThrowsAsync(exceptionType, () =>
            FleetRegionUpdateHandler.FleetRegionUpdateAsync(
                input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                CancellationToken.None
                )
        );

        TestsHelper.VerifyLoggerWasCalled(MockLogger!, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Never);
        return Task.CompletedTask;
    }
}
