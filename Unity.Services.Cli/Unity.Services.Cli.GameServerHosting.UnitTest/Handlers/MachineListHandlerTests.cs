using Moq;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Machine = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.Machine1;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

class MachineListHandlerTests : HandlerCommon
{
    [Test]
    public async Task MachineListAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await MachineListHandler.MachineListAsync(
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
    public async Task MachineListAsync_CallsFetchIdentifierAsync()
    {
        MachineListInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName
        };

        await MachineListHandler.MachineListAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(
        null,
        null,
        null,
        null)]
    [TestCase(
        InvalidFleetId,
        "CLOUD",
        ValidMachineId,
        Machine.StatusEnum.ONLINE)]
    [TestCase(
        ValidFleetId,
        "CLOUD",
        InvalidMachineId,
        Machine.StatusEnum.ONLINE)]
    [TestCase(
        ValidFleetId,
        "CLOUD",
        ValidMachineId,
        Machine.StatusEnum.ONLINE)]
    public async Task MachineListAsync_CallsListService(
        string? fleetId,
        string hardwareType,
        long? partial,
        Machine.StatusEnum? status
    )
    {
        Guid? fleetGuid = fleetId == null ? null : new Guid(fleetId);

        var partialString = partial == null ? null : partial.ToString();

        var statusString = status == null ? null : status.ToString();

        MachineListInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            FleetId = fleetId,
            HardwareType = hardwareType,
            Partial = partialString,
            Status = statusString
        };

        await MachineListHandler.MachineListAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MachinesApi!.DefaultMachinesClient.Verify(
            api => api.ListMachinesAsync(
                new Guid(input.CloudProjectId),
                new Guid(ValidEnvironmentId),
                null,
                null,
                null,
                null,
                null,
                fleetGuid,
                null,
                hardwareType,
                partialString,
                statusString,
                0,
                CancellationToken.None
            ),
            Times.Once
        );
    }
}
