using System.Net;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using WireMock.Admin.Mappings;
using WireMock.Net.OpenApiParser.Settings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Unity.Services.Cli.MockServer.ServiceMocks.GameServerHosting;

public class GameServerHostingApiMock : IServiceApiMock
{
    public async Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        var models = await MappingModelUtils.ParseMappingModelsAsync(
            "https://services.docs.unity.com/specs/v1/6d756c7469706c61792d636f6e666967.yaml",
            new WireMockOpenApiParserSettings()
        );

        models = models.Select(
            m => m
                .ConfigMappingPathWithKey(CommonKeys.ProjectIdKey, CommonKeys.ValidProjectId)
                .ConfigMappingPathWithKey(CommonKeys.EnvironmentIdKey, CommonKeys.ValidEnvironmentId)
        );

        return models.ToArray();
    }

    public void CustomMock(WireMockServer mockServer)
    {
        MockFleetGet(mockServer);
        MockServerGet(mockServer);
        MockServerList(mockServer);
        MockBuildInstalls(mockServer);
        MockBuild(mockServer);
        MockBuildList(mockServer);
        MockBuildCreateResponse(mockServer);
    }

    static void MockFleetGet(WireMockServer mockServer)
    {
        var fleet = new Fleet(
            name: "Test Fleet",
            id: Guid.Parse(Keys.ValidFleetId),
            osFamily: Fleet.OsFamilyEnum.LINUX,
            osName: "Ubuntu 20.04",
            status: Fleet.StatusEnum.OFFLINE,
            buildConfigurations: new List<BuildConfiguration2>(),
            fleetRegions: new List<FleetRegion1>(),
            servers: new Servers(
                new FleetServerBreakdown(new ServerStatus()),
                new FleetServerBreakdown(new ServerStatus()),
                new FleetServerBreakdown(new ServerStatus())
            ),
            allocationTTL: 0,
            deleteTTL: 0,
            disabledDeleteTTL: 0,
            shutdownTTL: 0
        );

        var request = Request.Create()
            .WithPath(Keys.ValidFleetPath)
            .UsingGet();

        var response = Response.Create()
            .WithBodyAsJson(fleet)
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(request).RespondWith(response);
    }

    static void MockServerGet(WireMockServer mockServer)
    {
        var server = new Server(
            buildConfigurationID: Keys.ValidBuildConfigurationId,
            buildConfigurationName: "Test Build Configuration",
            buildName: "Test Build",
            deleted: false,
            fleetID: Guid.Parse(Keys.ValidFleetId),
            fleetName: "Test Fleet",
            hardwareType: Server.HardwareTypeEnum.CLOUD,
            id: Int64.Parse(Keys.ValidServerId),
            ip: "1.1.1.1",
            locationID: 0,
            locationName: "Test Location",
            machineID: 123,
            port: 0,
            status: Server.StatusEnum.ONLINE
        );

        var request = Request.Create()
            .WithPath(Keys.ValidServersPath)
            .UsingGet();

        var response = Response.Create()
            .WithBodyAsJson(server);

        mockServer.Given(request).RespondWith(response);
    }

    static void MockServerList(WireMockServer mockServer)
    {
        var servers = new List<Server>
        {
            new(
                buildConfigurationID: Keys.ValidBuildConfigurationId,
                buildConfigurationName: "Test Build Configuration",
                buildName: "Test Build",
                deleted: false,
                fleetID: Guid.Parse(Keys.ValidFleetId),
                fleetName: "Test Fleet",
                hardwareType: Server.HardwareTypeEnum.CLOUD,
                id: Int64.Parse(Keys.ValidServerId),
                ip: "1.1.1.1",
                locationID: 0,
                locationName: "Test Location",
                machineID: 123,
                port: 0,
                status: Server.StatusEnum.ONLINE
            )
        };

        var request = Request.Create()
            .WithPath(Keys.ServersPath)
            .UsingGet();

        var response = Response.Create()
            .WithBodyAsJson(servers)
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(request).RespondWith(response);
    }
    static void MockBuildList(WireMockServer mockServer)
    {
        Console.WriteLine(Keys.ValidBuildPath);
        var build = new List<BuildListInner>
        {
            new(
                1,
                11,
                "Build1",
                ccd: new CCDDetails(
                    new Guid(Keys.ValidBucketId),
                    new Guid(Keys.ValidRegionId)),
                osFamily: BuildListInner.OsFamilyEnum.LINUX,
                syncStatus: BuildListInner.SyncStatusEnum.SYNCED,
                updated: new DateTime(2022, 10, 11)),
            new(
                2,
                22,
                "Build2",
                container: new ContainerImage("v1"),
                osFamily: BuildListInner.OsFamilyEnum.LINUX,
                syncStatus: BuildListInner.SyncStatusEnum.SYNCED,
                updated: new DateTime(2022, 10, 11))
        };
        var request = Request.Create()
            .WithPath(Keys.BuildsPath)
            .UsingGet();

        var response = Response.Create()
            .WithBodyAsJson(build)
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(request).RespondWith(response);
    }

    static void MockBuild(WireMockServer mockServer)
    {
        Console.WriteLine(Keys.ValidBuildPath);
        var build = new CreateBuild200Response(
            1,
            "name1",
            CreateBuild200Response.BuildTypeEnum.S3,
            ccd: new CCDDetails()
        );
        var request = Request.Create()
            .WithPath(Keys.ValidBuildPath)
            .UsingGet();

        var response = Response.Create()
            .WithBodyAsJson(build)
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(request).RespondWith(response);
    }

    static void MockBuildInstalls(WireMockServer mockServer)
    {
        var buildInstalls = new List<BuildListInner1>
        {
            new BuildListInner1(
                new CCDDetails(
                    Guid.Parse(Keys.ValidBucketId),
                    Guid.Parse(Keys.ValidReleaseId)
                ),
                completedMachines: 1,
                container: new ContainerImage(
                    "tag"
                ),
                failures: new List<BuildListInner1FailuresInner>
                {
                    new(
                        1234,
                        "failure",
                        DateTime.Now
                    )
                },
                fleetName: "fleet name",
                pendingMachines: 1,
                regions: new List<RegionsInner>
                {
                    new(
                        1,
                        1,
                        1,
                        "region name"
                    )
                }
            )
        };
        var request = Request.Create()
            .WithPath(Keys.ValidBuildInstallsPath)
            .UsingGet();

        var response = Response.Create()
            .WithBodyAsJson(buildInstalls)
            .WithStatusCode(HttpStatusCode.OK);
        mockServer.Given(request).RespondWith(response);
    }

    static void MockBuildCreateResponse(WireMockServer mockServer)
    {
        var build = new CreateBuild200Response(
            1,
            "Build1",
            ccd: new CCDDetails(
                new Guid(Keys.ValidBucketId),
                new Guid(Keys.ValidReleaseId)
            ),
            osFamily: CreateBuild200Response.OsFamilyEnum.LINUX,
            syncStatus: CreateBuild200Response.SyncStatusEnum.SYNCED,
            updated: new DateTime(2022, 10, 11)
        );
        var request = Request.Create()
            .WithPath(Keys.BuildsPath)
            .UsingPost();

        var response = Response.Create()
            .WithBodyAsJson(build)
            .WithStatusCode(HttpStatusCode.OK);
        mockServer.Given(request).RespondWith(response);
    }
}
