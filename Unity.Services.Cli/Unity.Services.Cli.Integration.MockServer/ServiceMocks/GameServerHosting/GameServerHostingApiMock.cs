using System.Net;
using System.Text;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using WireMock.Admin.Mappings;
using WireMock.Net.OpenApiParser.Settings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using File = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.File;
using Machine = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.Machine1;

namespace Unity.Services.Cli.MockServer.ServiceMocks.GameServerHosting;

public class GameServerHostingApiMock : IServiceApiMock
{
    public const string TempFileName = "temp-file.txt";

    public async Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        var models = await MappingModelUtils.ParseMappingModelsAsync(
            "OpenApi/Specs/game-server-hosting/ignore-hidden-usg-v1-dereferenced.yaml",
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
        MockFleetList(mockServer);
        MockServerGet(mockServer);
        MockServerList(mockServer);
        MockBuildInstalls(mockServer);
        MockBuild(mockServer);
        MockBuildList(mockServer);
        MockBuildCreateResponse(mockServer);
        MockFiles(mockServer);
        MockFleetRegionCreateResponse(mockServer);
        MockFleetAvailableRegionsResponse(mockServer);
        MockBuildConfigurationCreate(mockServer);
        MockBuildConfigurationGet(mockServer);
        MockBuildConfigurationUpdate(mockServer);
        MockBuildConfigurationList(mockServer);
        MockMachineList(mockServer);
        MockFilesDownload(mockServer);
    }

    static void MockFleetList(WireMockServer mockServer)
    {
        var fleetList = new List<FleetListItem>
        {
            new (
                FleetListItem.AllocationTypeEnum.ALLOCATION,
                new List<BuildConfiguration1>(),
                graceful: false,
                regions: new List<FleetRegion>(),
                id: new Guid(Keys.ValidFleetId),
                name: "Test Fleet",
                osName: Fleet.OsFamilyEnum.LINUX.ToString(),
                servers: new Servers(
                    new FleetServerBreakdown(new ServerStatus()),
                    new FleetServerBreakdown(new ServerStatus()),
                    new FleetServerBreakdown(new ServerStatus())),
                status: FleetListItem.StatusEnum.ONLINE
            ),
        };

        var request = Request.Create()
            .WithPath(Keys.FleetsPath)
            .UsingGet();

        var response = Response.Create()
            .WithBodyAsJson(fleetList)
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(request).RespondWith(response);
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
            id: long.Parse(Keys.ValidServerId),
            ip: "1.1.1.1",
            locationID: 0,
            locationName: "Test Location",
            machineID: 123,
            machineName: "test machine",
            machineSpec: new MachineSpec1(
                contractEndDate: new DateTime(
                    2020,
                    12,
                    31,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc),
                contractStartDate: new DateTime(
                    2020,
                    1,
                    1,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc),
                cpuName: "test-cpu",
                cpuShortname: "tc"
            ),
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
                id: long.Parse(Keys.ValidServerId),
                ip: "1.1.1.1",
                locationID: 0,
                locationName: "Test Location",
                machineID: 123,
                machineName: "test machine",
                machineSpec: new MachineSpec1(
                    contractEndDate: new DateTime(
                        2020,
                        12,
                        31,
                        12,
                        0,
                        0,
                        DateTimeKind.Utc),
                    contractStartDate: new DateTime(
                        2020,
                        1,
                        1,
                        12,
                        0,
                        0,
                        DateTimeKind.Utc),
                    cpuName: "test-cpu",
                    cpuShortname: "tc"
                ),
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

    static void MockFiles(WireMockServer mockServer)
    {
        var files = new List<File>
        {
            new(
                filename: "server.log",
                path: "logs/",
                fileSize: 100,
                createdAt: new DateTime(2022, 10, 11),
                lastModified: new DateTime(2022, 10, 12),
                fleet: new FleetDetails(
                    id: new Guid(Keys.ValidFleetId),
                    name: "Test Fleet"
                ),
                machine: new Gateway.GameServerHostingApiV1.Generated.Model.Machine(
                    id: Keys.ValidMachineId,
                    location: "europe-west1"
                ),
                serverID: 123
            )
        };

        var request = Request.Create()
            .WithPath(Keys.FilesPath)
            .UsingGet();

        var response = Response.Create()
            .WithBodyAsJson(files)
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(request).RespondWith(response);
    }

    static void MockBuildList(WireMockServer mockServer)
    {
        var build = new List<BuildListInner>
        {
            new(
                1,
                11,
                "Build1",
                buildVersionName: "v1",
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
                buildVersionName: "v1",
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
        var build = new CreateBuild200Response(
            1,
            "name1",
            CreateBuild200Response.BuildTypeEnum.S3,
            "v1",
            ccd: new CCDDetails()
        );
        var request = Request.Create()
            .WithPath(Keys.ValidBuildPath)
            .UsingGet();

        var response = Response.Create()
            .WithBodyAsJson(build)
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(request).RespondWith(response);

        MockBuildBucket(mockServer);
        MockBuildContainer(mockServer);
        MockBuildFileUpload(mockServer);
    }

    static void MockBuildBucket(WireMockServer mockServer)
    {
        var build = new CreateBuild200Response(
            Keys.ValidBuildIdBucket,
            "Bucket Build",
            CreateBuild200Response.BuildTypeEnum.S3,
            "v1",
            s3: new AmazonS3Details("bucket-url")
        );

        var request = Request.Create()
            .WithPath(Keys.ValidBuildPathBucket)
            .UsingGet();

        var response = Response.Create()
            .WithBodyAsJson(build)
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(request).RespondWith(response);
    }

    static void MockBuildContainer(WireMockServer mockServer)
    {
        var build = new CreateBuild200Response(
            Keys.ValidBuildIdContainer,
            "Container Build",
            CreateBuild200Response.BuildTypeEnum.CONTAINER,
            "v1",
            container: new ContainerImage("v1")
        );

        var request = Request.Create()
            .WithPath(Keys.ValidBuildPathContainer)
            .UsingGet();

        var response = Response.Create()
            .WithBodyAsJson(build)
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(request).RespondWith(response);
    }

    static void MockBuildFileUpload(WireMockServer mockServer)
    {
        var build = new CreateBuild200Response(
            Keys.ValidBuildIdFileUpload,
            "File Upload Build",
            CreateBuild200Response.BuildTypeEnum.FILEUPLOAD,
            "v1",
            ccd: new CCDDetails()
        );

        var request = Request.Create()
            .WithPath(Keys.ValidBuildPathFileUpload)
            .UsingGet();

        var response = Response.Create()
            .WithBodyAsJson(build)
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(request).RespondWith(response);

        const string signedUrlPath = $"/signedUrl/{TempFileName}";
        var file = new BuildFilesListResultsInner(
            "fake-hash",
            TempFileName,
            $"{mockServer.Url}{signedUrlPath}"
        );


        var fileRequest = Request.Create()
            .WithPath(Keys.ValidBuildPathFileUploadFiles)
            .UsingPut();

        var fileResponse = Response.Create()
            .WithBodyAsJson(file)
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(fileRequest).RespondWith(fileResponse);

        var signedUrlRequest = Request.Create()
            .WithPath(signedUrlPath)
            .UsingPut();

        var signedUrlResponse = Response.Create()
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(signedUrlRequest).RespondWith(signedUrlResponse);
    }

    static void MockBuildInstalls(WireMockServer mockServer)
    {
        var buildInstalls = new List<BuildListInner1>
        {
            new BuildListInner1(
                Keys.ValidBuildVersionName,
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
            CreateBuild200Response.BuildTypeEnum.FILEUPLOAD,
            "v1",
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

    static void MockBuildConfigurationCreate(WireMockServer mockServer)
    {
        var buildConfig = new BuildConfiguration(
            binaryPath: "simple-game-server-go",
            buildID: Keys.ValidBuildConfigurationId,
            buildName: "Build 1",
            commandLine: "--init game.init",
            configuration: new List<ConfigEntry>(),
            cores: 1,
            createdAt: new DateTime(2022, 10, 11),
            fleetID: new Guid(Keys.ValidFleetId),
            fleetName: "Fleet 1",
            id: 1120778,
            memory: 100,
            name: "Testing BC",
            queryType: "sqp",
            speed: 100,
            updatedAt: new DateTime(2022, 10, 11),
            version: 5
        );

        var request = Request.Create()
            .WithPath(Keys.BuildConfigurationsPath)
            .UsingPost();

        var response = Response.Create()
            .WithBodyAsJson(buildConfig)
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(request).RespondWith(response);
    }

    static void MockBuildConfigurationGet(WireMockServer mockServer)
    {
        var buildConfig = new BuildConfiguration(
            binaryPath: "simple-game-server-go",
            buildID: Keys.ValidBuildConfigurationId,
            buildName: "Build 1",
            commandLine: "--init game.init",
            configuration: new List<ConfigEntry>(),
            cores: 1,
            createdAt: new DateTime(2022, 10, 11),
            fleetID: new Guid(Keys.ValidFleetId),
            fleetName: "Fleet 1",
            id: 1120778,
            memory: 100,
            name: "Testing BC",
            queryType: "sqp",
            speed: 100,
            updatedAt: new DateTime(2022, 10, 11),
            version: 5
        );

        var request = Request.Create()
            .WithPath(Keys.ValidBuildConfigurationPath)
            .UsingGet();

        var response = Response.Create()
            .WithBodyAsJson(buildConfig)
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(request).RespondWith(response);
    }

    static void MockBuildConfigurationUpdate(WireMockServer mockServer)
    {
        var buildConfig = new BuildConfiguration(
            binaryPath: "simple-game-server-go",
            buildID: Keys.ValidBuildConfigurationId,
            buildName: "Build 1",
            commandLine: "--init game.init",
            configuration: new List<ConfigEntry>(),
            cores: 1,
            createdAt: new DateTime(2022, 10, 11),
            fleetID: new Guid(Keys.ValidFleetId),
            fleetName: "Fleet 1",
            id: 1120778,
            memory: 100,
            name: "Testing BC",
            queryType: "sqp",
            speed: 100,
            updatedAt: new DateTime(2022, 10, 11),
            version: 5
        );

        var request = Request.Create()
            .WithPath(Keys.ValidBuildConfigurationPath)
            .UsingPost();

        var response = Response.Create()
            .WithBodyAsJson(buildConfig)
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(request).RespondWith(response);
    }

    static void MockBuildConfigurationList(WireMockServer mockServer)
    {
        var buildConfigs = new List<BuildConfigurationListItem>
        {
            new(
                1,
                "build config 1",
                new DateTime(2022, 10, 11),
                new Guid(Keys.ValidFleetId),
                "fleet name",
                11,
                "config name",
                new DateTime(2022, 10, 11),
                1
            ),
            new(
                2,
                "build config 2",
                new DateTime(2022, 10, 11),
                new Guid(Keys.ValidFleetId),
                "fleet name",
                11,
                "config name",
                new DateTime(2022, 10, 11),
                1
            ),
        };

        var request = Request.Create()
            .WithPath(Keys.BuildConfigurationsPath)
            .UsingGet();

        var response = Response.Create()
            .WithBodyAsJson(buildConfigs)
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(request).RespondWith(response);
    }

    static void MockFleetRegionCreateResponse(WireMockServer mockServer)
    {
        var build = new NewFleetRegion(
            new Guid(Keys.ValidFleetRegionId),
            2,
            1,
            regionID: new Guid(Keys.ValidRegionId),
            regionName: "region name"
        );
        var request = Request.Create()
            .WithPath(Keys.FleetRegionsPath)
            .UsingPost();

        var response = Response.Create()
            .WithBodyAsJson(build)
            .WithStatusCode(HttpStatusCode.OK);
        mockServer.Given(request).RespondWith(response);
    }

    static void MockFleetAvailableRegionsResponse(WireMockServer mockServer)
    {
        var resp = new List<FleetRegionsTemplateListItem>
        {
            new(
                "US East",
                new Guid(Keys.ValidRegionId)
            ),
            new(
                "US West",
                new Guid(Keys.ValidRegionIdAlt)
            )
        };
        var request = Request.Create()
            .WithPath(Keys.AvailableRegionsPath)
            .UsingGet();

        var response = Response.Create()
            .WithBodyAsJson(resp)
            .WithStatusCode(HttpStatusCode.OK);
        mockServer.Given(request).RespondWith(response);
    }

    static void MockMachineList(WireMockServer mockServer)
    {
        var machines = new List<Machine>
        {
            new(
                id: Keys.ValidMachineId,
                ip: "127.0.0.10",
                name: "p-gce-test-2",
                locationId: 111111,
                locationName: "us-west1",
                fleetId: new Guid(Keys.ValidFleetId),
                fleetName: "Fleet One",
                hardwareType: Machine.HardwareTypeEnum.CLOUD,
                osFamily: Machine.OsFamilyEnum.LINUX,
                osName: "Ubuntu (Server) 22.04 LTS",
                serversStates: new ServersStates(
                    0,
                    5,
                    2,
                    1
                ),
                spec: new MachineSpec(
                    8,
                    "U1.Standard.3",
                    2800,
                    "Cloud Intel 2nd Gen Scalable",
                    32212254720
                ),
                status: Machine.StatusEnum.ONLINE,
                deleted: false,
                disabled: false
            )
        };

        var request = Request.Create()
            .WithPath(Keys.MachinesPath)
            .UsingGet();

        var response = Response.Create()
            .WithBodyAsJson(machines)
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(request).RespondWith(response);
    }

    static void MockFilesDownload(WireMockServer mockServer)
    {
        var contentUrl = new GenerateContentURLResponse(
            url: $"{mockServer.Url}{Keys.MockedSignedUrlPath}"
        );

        var requestSignedUrlJson = new GenerateContentURLRequest(
            path: Keys.ValidErrorLogPath,
            serverId: long.Parse(Keys.ValidServerId)
        );

        var requestSignedUrl = Request.Create()
            .WithPath(Keys.GenerateDownloadUrlPath)
            .WithBodyAsJson(requestSignedUrlJson)
            .UsingPost();

        var responseSignedUrl = Response.Create()
            .WithBodyAsJson(contentUrl)
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(requestSignedUrl).RespondWith(responseSignedUrl);

        var fileContentBytes = Encoding.UTF8.GetBytes(Keys.MockFileContent);

        var request = Request.Create()
            .WithPath(Keys.MockedSignedUrlPath)
            .UsingGet();

        var response = Response.Create()
            .WithBody(fileContentBytes)
            .WithStatusCode(HttpStatusCode.OK);

        mockServer.Given(request).RespondWith(response);
    }
}
