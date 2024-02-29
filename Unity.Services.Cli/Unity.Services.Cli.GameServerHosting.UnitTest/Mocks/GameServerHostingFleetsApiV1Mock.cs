using Moq;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Client;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Mocks;

class GameServerHostingFleetsApiV1Mock
{
    static readonly List<FleetListItem> k_TestFleetItems = new()
    {
        new FleetListItem(
            FleetListItem.AllocationTypeEnum.ALLOCATION,
            new List<BuildConfiguration1>(),
            graceful: false,
            regions: new List<FleetRegion>(),
            id: new Guid(ValidFleetId),
            name: ValidFleetName,
            osName: OsNameLinux,
            servers: new Servers(new FleetServerBreakdown(new ServerStatus()),
                new FleetServerBreakdown(new ServerStatus()), new FleetServerBreakdown(new ServerStatus())),
            status: FleetListItem.StatusEnum.ONLINE
        ),
        new FleetListItem(
            FleetListItem.AllocationTypeEnum.ALLOCATION,
            new List<BuildConfiguration1>(),
            graceful: false,
            regions: new List<FleetRegion>(),
            id: new Guid(ValidFleetId2),
            name: ValidFleetName2,
            osName: OsNameLinux,
            servers: new Servers(new FleetServerBreakdown(new ServerStatus()),
                new FleetServerBreakdown(new ServerStatus()), new FleetServerBreakdown(new ServerStatus())),
            status: FleetListItem.StatusEnum.ONLINE
        )
    };

    static readonly List<FleetRegion1> k_TestFleetRegions = new()
    {
        new FleetRegion1(
            deleteTTL: 120,
            disabledDeleteTTL: 60,
            id: new Guid(ValidFleetId),
            maxServers: 3,
            minAvailableServers: 3,
            regionID: new Guid(ValidRegionId),
            regionName: ValidTemplateRegionName,
            scalingEnabled: false,
            shutdownTTL: 180
        ),
        new FleetRegion1(
            deleteTTL: 120,
            disabledDeleteTTL: 60,
            id: new Guid(ValidFleetId2),
            maxServers: 3,
            minAvailableServers: 3,
            regionID: new Guid(ValidRegionId2),
            regionName: ValidTemplateRegionName2,
            scalingEnabled: false,
            shutdownTTL: 180
        )
    };

    static readonly List<Fleet> k_TestFleets = new()
    {
        new Fleet(
            buildConfigurations: new List<BuildConfiguration2>()
            {
                new(id: 1, name: "build config 1", buildName: "build 1", buildID: 1)
            },
            graceful: false,
            fleetRegions: k_TestFleetRegions,
            id: new Guid(ValidFleetId),
            name: ValidFleetName,
            osFamily: Fleet.OsFamilyEnum.LINUX,
            osName: OsNameLinux,
            servers: new Servers(new FleetServerBreakdown(new ServerStatus()),
                new FleetServerBreakdown(new ServerStatus()), new FleetServerBreakdown(new ServerStatus())),
            status: Fleet.StatusEnum.ONLINE,
            allocationTTL: 10,
            deleteTTL: 20,
            disabledDeleteTTL: 25,
            shutdownTTL: 30
        ),
        new Fleet(
            buildConfigurations: new List<BuildConfiguration2>(),
            graceful: false,
            fleetRegions: new List<FleetRegion1>(),
            id: new Guid(ValidFleetId2),
            name: ValidFleetName2,
            osFamily: Fleet.OsFamilyEnum.LINUX,
            osName: OsNameLinux,
            servers: new Servers(new FleetServerBreakdown(new ServerStatus()),
                new FleetServerBreakdown(new ServerStatus()), new FleetServerBreakdown(new ServerStatus())),
            status: Fleet.StatusEnum.ONLINE,
            allocationTTL: 1,
            deleteTTL: 2,
            disabledDeleteTTL: 3,
            shutdownTTL: 4
        )
    };

    static readonly List<FleetRegionsTemplateListItem> k_TestFleetTemplateRegions = new()
    {
        new FleetRegionsTemplateListItem(
            name: ValidTemplateRegionName,
            regionID: new Guid(ValidTemplateRegionId)
        ),
        new FleetRegionsTemplateListItem(
            name: ValidTemplateRegionName2,
            regionID: new Guid(ValidTemplateRegionId2)
        )
    };

    static readonly List<FleetRegionsTemplateListItem> k_TestAvailableRegions = new()
    {
        new FleetRegionsTemplateListItem(
            name: ValidTemplateRegionName,
            regionID: new Guid(ValidTemplateRegionId)
        ),
        new FleetRegionsTemplateListItem(
            name: ValidTemplateRegionName2,
            regionID: new Guid(ValidTemplateRegionId2)
        )
    };

    static readonly NewFleetRegion k_TestNewFleetRegion = new(
        new Guid(ValidTemplateRegionId),
        1,
        2,
        new Guid(ValidTemplateRegionId),
        regionName: ValidTemplateRegionName
    );

    static readonly UpdatedFleetRegion k_UpdatedFleetRegion = new(
        deleteTTL: 120,
        disabledDeleteTTL: 60,
        id: Guid.Parse(ValidFleetId),
        maxServers: 3,
        minAvailableServers: 3,
        regionID: Guid.Parse(ValidRegionId),
        regionName: ValidTemplateRegionName,
        scalingEnabled: false,
        shutdownTTL: 180
    );

    public Mock<IFleetsApi> DefaultFleetsClient = new();

    public List<Guid>? ValidEnvironments;

    public List<Guid>? ValidProjects;

    public void SetUp()
    {
        DefaultFleetsClient = new Mock<IFleetsApi>();
        DefaultFleetsClient.Setup(a => a.Configuration)
            .Returns(new Configuration());

        DefaultFleetsClient.Setup(a =>
            a.GetFleetAsync(
                It.IsAny<Guid>(), // projectId
                It.IsAny<Guid>(), // environmentId
                It.IsAny<Guid>(), // fleetId
                0,
                CancellationToken.None
            )).Returns((Guid projectId, Guid environmentId, Guid fleetId, int _, CancellationToken _) =>
        {
            var validated = ValidateProjectEnvironment(projectId, environmentId);
            if (!validated) throw new HttpRequestException();

            var fleet = k_TestFleets.Find(b => b.Id == fleetId);
            if (fleet == null) throw new HttpRequestException();

            return Task.FromResult(fleet);
        });

        DefaultFleetsClient.Setup(a => a.ListFleetsAsync(
            It.IsAny<Guid>(), // projectId
            It.IsAny<Guid>(), // environmentId
            0,
            CancellationToken.None
        )).Returns((Guid projectId, Guid environmentId, int _, CancellationToken _) =>
        {
            var validated = ValidateProjectEnvironment(projectId, environmentId);
            if (!validated) throw new HttpRequestException();
            return Task.FromResult(k_TestFleetItems);
        });

        DefaultFleetsClient.Setup(a =>
            a.CreateFleetAsync(
                It.IsAny<Guid>(), // projectId
                It.IsAny<Guid>(), // environmentId,
                It.IsAny<Guid?>(), // template fleet id
                It.IsAny<FleetCreateRequest>(),
                0,
                CancellationToken.None
            )).Returns((Guid projectId, Guid environmentId, Guid? _, FleetCreateRequest createReq, int _, CancellationToken _) =>
        {
            var validated = ValidateProjectEnvironment(projectId, environmentId);
            if (!validated) throw new HttpRequestException();

            var fleet = new Fleet(
                buildConfigurations: new List<BuildConfiguration2>(),
                fleetRegions: new List<FleetRegion1>(),
                graceful: false,
                id: new Guid(ValidFleetId),
                name: createReq.Name,
                osFamily: (Fleet.OsFamilyEnum)createReq.OsFamily!,
                osName: OsNameLinux,
                servers: new Servers(new FleetServerBreakdown(new ServerStatus()),
                    new FleetServerBreakdown(new ServerStatus()), new FleetServerBreakdown(new ServerStatus())),
                status: Fleet.StatusEnum.ONLINE,
                allocationTTL: 10,
                deleteTTL: 20,
                disabledDeleteTTL: 25,
                shutdownTTL: 30
            );

            return Task.FromResult(fleet);
        });

        DefaultFleetsClient.Setup(a =>
            a.CreateFleetAsync(
                It.IsAny<Guid>(), // projectId
                It.IsAny<Guid>(), // environmentId
                It.IsAny<Guid?>(), // template Fleet id
                It.IsAny<FleetCreateRequest>(),
                0,
                CancellationToken.None
            )).Returns((Guid projectId, Guid environmentId, Guid? _, FleetCreateRequest createReq, int _, CancellationToken _) =>
        {
            var validated = ValidateProjectEnvironment(projectId, environmentId);
            if (!validated) throw new HttpRequestException();

            var fleet = new Fleet(
                buildConfigurations: new List<BuildConfiguration2>(),
                fleetRegions: new List<FleetRegion1>(),
                id: new Guid(ValidFleetId),
                graceful: false,
                name: createReq.Name,
                osFamily: (Fleet.OsFamilyEnum)createReq.OsFamily!,
                osName: OsNameLinux,
                servers: new Servers(new FleetServerBreakdown(new ServerStatus()),
                    new FleetServerBreakdown(new ServerStatus()), new FleetServerBreakdown(new ServerStatus())),
                status: Fleet.StatusEnum.ONLINE,
                allocationTTL: 10,
                deleteTTL: 20,
                disabledDeleteTTL: 25,
                shutdownTTL: 30
            );

            return Task.FromResult(fleet);
        });

        DefaultFleetsClient.Setup(a => a.DeleteFleetAsync(
            It.IsAny<Guid>(), // projectId
            It.IsAny<Guid>(), // environmentId
            It.IsAny<Guid>(), // fleetId
            null, // Dry Run
            0,
            CancellationToken.None
        )).Returns((Guid projectId, Guid environmentId, Guid fleetId, bool _, int _, CancellationToken _) =>
        {
            var validated = ValidateProjectEnvironment(projectId, environmentId);
            if (!validated) throw new HttpRequestException();

            var fleet = GetFleetById(fleetId);
            if (fleet is null) throw new HttpRequestException();

            return Task.CompletedTask;
        });

        DefaultFleetsClient.Setup(a => a.UpdateFleetAsync(
            It.IsAny<Guid>(), // projectId
            It.IsAny<Guid>(), // environmentId
            It.IsAny<Guid>(), // fleetId
            It.IsAny<FleetUpdateRequest>(), // update fleet request
            0,
            CancellationToken.None
        )).Returns((
            Guid projectId,
            Guid environmentId,
            Guid fleetId,
            FleetUpdateRequest _,
            int _,
            CancellationToken _
        ) =>
        {
            var validated = ValidateProjectEnvironment(projectId, environmentId);
            if (!validated) throw new HttpRequestException();

            var fleet = GetFleetById(fleetId);
            if (fleet == null) throw new ApiException();

            return Task.FromResult(fleet);
        });

        DefaultFleetsClient.Setup(a => a.ListTemplateFleetRegionsAsync(
            It.IsAny<Guid>(), // projectId
            It.IsAny<Guid>(), // environmentId
            It.IsAny<Guid?>(), // template Fleet id
            0,
            CancellationToken.None
        )).Returns((Guid projectId, Guid environmentId, Guid? _, int _, CancellationToken _) =>
        {
            var validated = ValidateProjectEnvironment(projectId, environmentId);
            if (!validated) throw new HttpRequestException();
            return Task.FromResult(k_TestFleetTemplateRegions);
        });

        DefaultFleetsClient.Setup(a => a.GetAvailableFleetRegionsAsync(
            It.IsAny<Guid>(), // projectId
            It.IsAny<Guid>(), // environmentId
            It.IsAny<Guid>(), // fleetId
            It.IsAny<Guid?>(), // template Fleet id
            0,
            CancellationToken.None
        )).Returns((Guid projectId, Guid environmentId, Guid fleetId, Guid? _, int _, CancellationToken _) =>
        {
            var validated = ValidateProjectEnvironment(projectId, environmentId);
            if (!validated) throw new HttpRequestException();

            var fleet = k_TestFleets.Find(b => b.Id == fleetId);
            if (fleet == null) throw new HttpRequestException();

            return Task.FromResult(k_TestAvailableRegions);
        });

        DefaultFleetsClient.Setup(a => a.AddFleetRegionAsync(
            It.IsAny<Guid>(), // projectId
            It.IsAny<Guid>(), // environmentId
            It.IsAny<Guid>(), // fleetId
            It.IsAny<Guid?>(), // template Fleet id
            It.IsAny<AddRegionRequest>(),
            0,
            CancellationToken.None
        )).Returns((Guid projectId, Guid environmentId, Guid fleetId, Guid? _, AddRegionRequest _, int _, CancellationToken _) =>
        {
            var validated = ValidateProjectEnvironment(projectId, environmentId);
            if (!validated) throw new HttpRequestException();

            var fleet = k_TestFleets.Find(b => b.Id == fleetId);
            if (fleet == null) throw new HttpRequestException();

            return Task.FromResult(k_TestNewFleetRegion);
        });

        DefaultFleetsClient.Setup(a => a.UpdateFleetRegionAsync(
            It.IsAny<Guid>(), // projectId
            It.IsAny<Guid>(), // environmentId
            It.IsAny<Guid>(), // fleetId
            It.IsAny<Guid>(), // regionId
            It.IsAny<UpdateRegionRequest>(),
            0,
            CancellationToken.None
            )).Returns((Guid projectId, Guid environmentId, Guid fleetId, Guid regionId, UpdateRegionRequest _, int _, CancellationToken _) =>
            {
                var validated = ValidateProjectEnvironment(projectId, environmentId);
                if (!validated) throw new HttpRequestException();

                var fleet = k_TestFleets.Find(b => b.Id == fleetId);
                if (fleet == null) throw new HttpRequestException();

                var region = fleet.FleetRegions.Find(r => r.RegionID == regionId);
                if (region == null) throw new HttpRequestException();

                return Task.FromResult(k_UpdatedFleetRegion);
            }
        );
    }

    bool ValidateProjectEnvironment(Guid projectId, Guid environmentId)
    {
        if (ValidProjects != null && !ValidProjects.Contains(projectId)) return false;
        if (ValidEnvironments != null && !ValidEnvironments.Contains(environmentId)) return false;
        return true;
    }

    static Fleet? GetFleetById(Guid id)
    {
        return k_TestFleets.FirstOrDefault(f => f.Id.Equals(id));
    }
}
