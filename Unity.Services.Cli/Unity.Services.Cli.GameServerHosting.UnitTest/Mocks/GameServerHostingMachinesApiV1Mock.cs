using Moq;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Client;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using Machine = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.Machine1;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Mocks;

public class GameServerHostingMachinesApiV1Mock
{
    readonly List<Machine> m_TestMachines = new()
    {
        new Machine(
            id: ValidMachineId,
            ip: "127.0.0.10",
            name: ValidMachineName,
            locationId: ValidLocationId,
            locationName: ValidLocationName,
            fleetId: new Guid(ValidFleetId),
            fleetName: ValidFleetName,
            hardwareType: Machine.HardwareTypeEnum.CLOUD,
            osFamily: Machine.OsFamilyEnum.LINUX,
            osName: OsNameFullNameLinux,
            serversStates: new ServersStates(
                allocated: 1,
                available: 2,
                held: 3,
                online: 4,
                reserved: 5
            ),
            spec: new MachineSpec(
                cpuCores: 1,
                cpuShortname:  ValidMachineCpuSeriesShortname,
                cpuSpeed: 1000,
                cpuType:  ValidMachineCpuType,
                memory:100000
            ),
            status: Machine.StatusEnum.ONLINE,
            deleted: false,
            disabled: false
        )
    };

    public Mock<IMachinesApi> DefaultMachinesClient = new();

    public List<Guid>? ValidEnvironments;

    public List<Guid>? ValidProjects;

    public void SetUp()
    {
        DefaultMachinesClient = new Mock<IMachinesApi>();
        DefaultMachinesClient.Setup(a => a.Configuration)
            .Returns(new Configuration());

        DefaultMachinesClient.Setup(
                a => a.ListMachinesAsync(
                    It.IsAny<Guid>(), // projectId
                    It.IsAny<Guid>(), // environmentId
                    It.IsAny<string?>(), // limit
                    It.IsAny<Guid?>(), // lastId
                    It.IsAny<string?>(), // lastValue
                    It.IsAny<string?>(), // sortBy
                    It.IsAny<string?>(), // sortDirection
                    It.IsAny<Guid?>(), // fleetId
                    It.IsAny<string?>(), // locationId
                    It.IsAny<string?>(), // hardwareType
                    It.IsAny<string?>(), // partial
                    It.IsAny<string?>(), // status
                    0,
                    CancellationToken.None
                ))
            .Returns(
                (
                    Guid projectId,
                    Guid environmentId,
                    string? _,
                    Guid? _,
                    string? _,
                    string? _,
                    string? _,
                    Guid? fleetId,
                    string? _,
                    string? hardwareType,
                    string? partial,
                    string? status,
                    int _,
                    CancellationToken _
                ) =>
                {
                    var validated = ValidateProjectEnvironment(projectId, environmentId);
                    if (!validated) throw new HttpRequestException();

                    var results = m_TestMachines.AsEnumerable();

                    if (fleetId != null)
                    {
                        results = results.Where(a => a.FleetId == fleetId);
                    }

                    if (hardwareType != null)
                    {
                        var validHardwareType = Enum.TryParse(status, out Machine.StatusEnum hardwareTypeEnum);
                        if (!validHardwareType) throw new HttpRequestException();
                        results = results.Where(a => a.Status == hardwareTypeEnum);
                    }

                    if (partial != null)
                    {
                        results = results.Where(
                            a =>
                            {
                                var id = a.Id.ToString().Contains(partial);
                                var ip = a.Ip.Contains(partial);
                                var machine = a.Id.ToString().Contains(partial);
                                return id || ip || machine;
                            }
                        );
                    }

                    if (status != null)
                    {
                        var validStatus = Enum.TryParse(status, out Machine.StatusEnum statusEnum);
                        if (!validStatus) throw new HttpRequestException();
                        results = results.Where(a => a.Status == statusEnum);
                    }

                    return Task.FromResult(results.ToList());
                }
            );
    }

    bool ValidateProjectEnvironment(Guid projectId, Guid environmentId)
    {
        if (ValidProjects != null && !ValidProjects.Contains(projectId)) return false;
        if (ValidEnvironments != null && !ValidEnvironments.Contains(environmentId)) return false;
        return true;
    }
}
