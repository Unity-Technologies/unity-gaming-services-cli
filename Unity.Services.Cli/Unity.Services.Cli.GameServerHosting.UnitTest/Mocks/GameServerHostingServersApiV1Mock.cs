using Moq;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Client;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using Machine = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.Machine1;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Mocks;

class GameServerHostingServersApiV1Mock
{
    readonly List<Server> m_TestServers = new()
    {
        new Server(
            id: ValidServerId,
            ip: "0.0.0.0",
            port: 9000,
            machineID: ValidMachineId,
            locationID: ValidLocationId,
            locationName: ValidLocationName,
            machineName:"",
            machineSpec: new MachineSpec1(
                contractEndDate: new DateTime(2020, 12, 31, 12, 0,0, DateTimeKind.Utc),
                contractStartDate: new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                cpuName: "test-cpu",
                cpuShortname: "tc"
            ),
            hardwareType: Server.HardwareTypeEnum.CLOUD,
            fleetID: new Guid(ValidFleetId),
            fleetName: ValidFleetName,
            buildConfigurationID: ValidBuildConfigurationId,
            buildConfigurationName: ValidBuildConfigurationName,
            buildName: ValidBuildName,
            deleted: false
        )
    };

    public Mock<IServersApi> DefaultServersClient = new();

    public List<Guid>? ValidEnvironments;

    public List<Guid>? ValidProjects;

    public void SetUp()
    {
        DefaultServersClient = new Mock<IServersApi>();
        DefaultServersClient.Setup(a => a.Configuration)
            .Returns(new Configuration());

        DefaultServersClient.Setup(
                a => a.GetServerAsync(
                    It.IsAny<Guid>(), // projectId
                    It.IsAny<Guid>(), // environmentId
                    It.IsAny<long>(), // serverID
                    0,
                    CancellationToken.None
                ))
            .Returns(
                (Guid projectId, Guid environmentId, long serverId, int _, CancellationToken _) =>
                {
                    var validated = ValidateProjectEnvironment(projectId, environmentId);
                    if (!validated) throw new HttpRequestException();

                    var server = m_TestServers.Find(b => b.Id == serverId);
                    if (server == null) throw new HttpRequestException();

                    return Task.FromResult(server);
                });

        DefaultServersClient.Setup(
                a => a.ListServersAsync(
                    It.IsAny<Guid>(), // projectId
                    It.IsAny<Guid>(), // environmentId
                    It.IsAny<string?>(), // limit
                    It.IsAny<Guid?>(), // lastId
                    It.IsAny<string?>(), // lastValue
                    It.IsAny<string?>(), // sortBy
                    It.IsAny<string?>(), // sortDirection
                    It.IsAny<Guid?>(), // fleetId
                    It.IsAny<string>(), // machineId
                    It.IsAny<string?>(), // locationId
                    It.IsAny<string?>(), // buildConfigurationId
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
                    string? _,
                    string? buildConfigurationId,
                    string? _,
                    string? partial,
                    string? status,
                    int _,
                    CancellationToken _
                ) =>
                {
                    var validated = ValidateProjectEnvironment(projectId, environmentId);
                    if (!validated) throw new HttpRequestException();

                    var results = m_TestServers.AsEnumerable();

                    if (fleetId != null)
                    {
                        results = results.Where(a => a.FleetID == fleetId);
                    }

                    if (buildConfigurationId != null)
                    {
                        var isLong = long.TryParse(buildConfigurationId, out var buildConfigurationIdString);
                        if (!isLong) throw new HttpRequestException();
                        results = results.Where(a => a.BuildConfigurationID == buildConfigurationIdString);
                    }

                    if (partial != null)
                    {
                        results = results.Where(
                            a =>
                            {
                                var id = a.Id.ToString().Contains(partial);
                                var ip = a.Ip.Contains(partial);
                                var machine = a.MachineID.ToString().Contains(partial);
                                return id || ip || machine;
                            }
                        );
                    }

                    if (status != null)
                    {
                        var validStatus = Enum.TryParse(status, out Server.StatusEnum statusEnum);
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
