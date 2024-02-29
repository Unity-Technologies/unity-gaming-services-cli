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
class FleetRegionCreateHandlerTests : HandlerCommon
{
    [Test]
    public async Task FleetRegionCreateAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await FleetRegionCreateHandler.FleetRegionCreateAsync(null!, MockUnityEnvironment.Object, null!, null!,
            mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task FleetRegionCreateAsync_CallsFetchIdentifierAsync()
    {
        FleetRegionCreateInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            FleetId = new Guid(ValidFleetId),
            RegionId = new Guid(ValidRegionId),
            MaxServers = 2,
            MinAvailableServers = 1,
        };


        await FleetRegionCreateHandler.FleetRegionCreateAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, ValidFleetId, ValidRegionId, 1, 2)]
    public async Task FleetRegionCreateAsync_CallsCreateService(string projectId, string environmentName, string fleetId,
        string regionId, long minAvailableServers, long maxServers)
    {
        FleetRegionCreateInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            FleetId = new Guid(fleetId),
            RegionId = new Guid(regionId),
            MinAvailableServers = minAvailableServers,
            MaxServers = maxServers,
        };

        await FleetRegionCreateHandler.FleetRegionCreateAsync(input, MockUnityEnvironment.Object, GameServerHostingService!,
            MockLogger!.Object, CancellationToken.None);

        var createRequest = new AddRegionRequest(maxServers: maxServers, minAvailableServers: minAvailableServers,
            regionID: new Guid(regionId));

        FleetsApi!.DefaultFleetsClient.Verify(api => api.AddFleetRegionAsync(
            new Guid(input.CloudProjectId), new Guid(ValidEnvironmentId),
            new Guid(fleetId), null, createRequest, 0, CancellationToken.None
        ), Times.Once);
    }

    [TestCase(InvalidProjectId, ValidEnvironmentName, null, ValidRegionId, 2, 1, TestName = "Null fleet")]
    [TestCase(ValidProjectId, InvalidEnvironmentId, ValidFleetId, null, 2, 1, TestName = "Null region")]
    [TestCase(ValidProjectId, ValidEnvironmentName, InvalidFleetId, ValidRegionId, null, 1, TestName = "Null max servers")]
    [TestCase(ValidProjectId, ValidEnvironmentName, InvalidFleetId, ValidRegionId, 2, null, TestName = "Null min available servers")]
    public Task FleetRegionCreateAsync_InvalidInputThrowsException(string? projectId, string? environmentName,
    string? fleetId, string? regionId, long? maxServers, long? minAvailableServers)
    {
        FleetRegionCreateInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            FleetId = fleetId == null ? null : new Guid(fleetId),
            RegionId = regionId == null ? null : new Guid(regionId),
            MaxServers = maxServers,
            MinAvailableServers = minAvailableServers,

        };

        Assert.ThrowsAsync<MissingInputException>(() =>
            FleetRegionCreateHandler.FleetRegionCreateAsync(input,
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
