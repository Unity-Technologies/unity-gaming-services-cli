using Moq;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Services;

public class FleetsClientTests
{
    Mock<IFleetsApiAsync>? m_MockApi;
    FleetClient? m_Client;

    [SetUp]
    public void SetUp()
    {
        m_MockApi = new Mock<IFleetsApiAsync>();
        m_Client = new FleetClient(m_MockApi.Object, new GameServerHostingApiConfig());
    }

    [Test]
    public async Task FindByName_WithNoResults_ReturnsNull()
    {
        m_MockApi!.Setup(a =>
                a.ListFleetsAsync(
                    Guid.Empty,
                    Guid.Empty,
                    default,
                    default))
            .ReturnsAsync(new List<FleetListItem>());

        var res = await m_Client!.FindByName("test");

        Assert.That(res, Is.Null);
    }

    [Test]
    public async Task FindByName_WithOneResult_ReturnsId()
    {
        m_MockApi!.Setup(a =>
                a.ListFleetsAsync(
                    Guid.Empty,
                    Guid.Empty,
                    default,
                    default))
            .ReturnsAsync(new List<FleetListItem>
            {
                new FleetListItem(
                    allocationType: FleetListItem.AllocationTypeEnum.ALLOCATION,
                    new List<BuildConfiguration1>(),
                    graceful: false,
                    Guid.Empty,
                    name: "test",
                    osName: string.Empty,
                    regions: new List<FleetRegion>(),
                    servers: new Servers(
                        all: new FleetServerBreakdown(new ServerStatus()),
                        cloud: new FleetServerBreakdown(new ServerStatus()),
                        metal: new FleetServerBreakdown(new ServerStatus())
                    )
                )
            });

        var res = await m_Client!.FindByName("test");

        Assert.That(res, Is.Not.Null);
    }

    [Test]
    public void FindByName_WithMultipleResults_ThrowsDuplicateException()
    {
        m_MockApi!.Setup(a =>
                a.ListFleetsAsync(
                    Guid.Empty,
                    Guid.Empty,
                    default,
                    default))
            .ReturnsAsync(new List<FleetListItem>
            {
                new FleetListItem(
                    allocationType: FleetListItem.AllocationTypeEnum.ALLOCATION,
                    new List<BuildConfiguration1>(),
                    graceful: false,
                    Guid.Empty,
                    name: "test",
                    osName: string.Empty,
                    regions: new List<FleetRegion>(),
                    servers: new Servers(
                        all: new FleetServerBreakdown(new ServerStatus()),
                        cloud: new FleetServerBreakdown(new ServerStatus()),
                        metal: new FleetServerBreakdown(new ServerStatus())
                    )
                ),
                new FleetListItem(
                    allocationType: FleetListItem.AllocationTypeEnum.ALLOCATION,
                    new List<BuildConfiguration1>(),
                    graceful: false,
                    Guid.Empty,
                    name: "test",
                    osName: string.Empty,
                    regions: new List<FleetRegion>(),
                    servers: new Servers(
                        all: new FleetServerBreakdown(new ServerStatus()),
                        cloud: new FleetServerBreakdown(new ServerStatus()),
                        metal: new FleetServerBreakdown(new ServerStatus())
                    )
                )
            });

        Assert.ThrowsAsync<DuplicateResourceException>(async () => await m_Client!.FindByName("test"));
    }

    [Test]
    public async Task Create_CallsCreateApi()
    {
        m_MockApi!.Setup(a =>
                a.ListTemplateFleetRegionsAsync(Guid.Empty, Guid.Empty, null, default, default))
            .ReturnsAsync(new List<FleetRegionsTemplateListItem>());

        m_MockApi.Setup(a =>
                a.CreateFleetAsync(Guid.Empty, Guid.Empty, null, It.IsAny<FleetCreateRequest>(), default, default))
            .ReturnsAsync(new Fleet(
                buildConfigurations: new List<BuildConfiguration2>(),
                fleetRegions: new List<FleetRegion1>(),
                name: "test",
                osName: string.Empty,
                servers: new Servers(
                    all: new FleetServerBreakdown(new ServerStatus()),
                    cloud: new FleetServerBreakdown(new ServerStatus()),
                    metal: new FleetServerBreakdown(new ServerStatus())
                )));

        await m_Client!.Create("test", new List<BuildConfigurationId>(), new MultiplayConfig.FleetDefinition());

        m_MockApi.Verify(a =>
            a.CreateFleetAsync(Guid.Empty, Guid.Empty, null, It.IsAny<FleetCreateRequest>(), default, default));
    }

    [Test]
    public async Task Update_CallsTheUpdateApi()
    {
        m_MockApi!.Setup(a =>
                a.ListTemplateFleetRegionsAsync(Guid.Empty, Guid.Empty, null, default, default))
            .ReturnsAsync(new List<FleetRegionsTemplateListItem>());

        m_MockApi.Setup(a =>
                a.UpdateFleetAsync(Guid.Empty, Guid.Empty, Guid.Empty, It.IsAny<FleetUpdateRequest>(), default, default))
            .ReturnsAsync(new Fleet(
                buildConfigurations: new List<BuildConfiguration2>(),
                fleetRegions: new List<FleetRegion1>(),
                name: "test",
                osName: string.Empty,
                servers: new Servers(
                    all: new FleetServerBreakdown(new ServerStatus()),
                    cloud: new FleetServerBreakdown(new ServerStatus()),
                    metal: new FleetServerBreakdown(new ServerStatus())
                )));

        await m_Client!.Update(new FleetId(), "test", new List<BuildConfigurationId>(), new MultiplayConfig.FleetDefinition());

        m_MockApi.Verify(a =>
            a.UpdateFleetAsync(Guid.Empty, Guid.Empty, Guid.Empty, It.IsAny<FleetUpdateRequest>(), default, default));
    }

    [Test]
    public async Task Update_RemovesOldFleets()
    {
        var regionId = Guid.NewGuid();
        m_MockApi!.Setup(a =>
                a.ListTemplateFleetRegionsAsync(Guid.Empty, Guid.Empty, null, default, default))
            .ReturnsAsync(new List<FleetRegionsTemplateListItem>());

        m_MockApi.Setup(a =>
                a.UpdateFleetAsync(Guid.Empty, Guid.Empty, Guid.Empty, It.IsAny<FleetUpdateRequest>(), default, default))
            .ReturnsAsync(new Fleet(
                buildConfigurations: new List<BuildConfiguration2>(),
                fleetRegions: new List<FleetRegion1>()
                {
                    new (regionID: regionId, regionName: string.Empty)
                },
                name: "test",
                osName: string.Empty,
                servers: new Servers(
                    all: new FleetServerBreakdown(new ServerStatus()),
                    cloud: new FleetServerBreakdown(new ServerStatus()),
                    metal: new FleetServerBreakdown(new ServerStatus())
                )));

        await m_Client!.Update(new FleetId(), "test", new List<BuildConfigurationId>(), new MultiplayConfig.FleetDefinition());

        m_MockApi.Verify(f => f.UpdateFleetRegionAsync(Guid.Empty, Guid.Empty, Guid.Empty, regionId, null, default, default));
    }

    [Test]
    public async Task Update_AddsNewFleets()
    {
        var regionId = Guid.NewGuid();
        m_MockApi!.Setup(a =>
                a.ListTemplateFleetRegionsAsync(Guid.Empty, Guid.Empty, null, default, default))
            .ReturnsAsync(new List<FleetRegionsTemplateListItem>
            {
                new ("North-America", regionId)
            });

        m_MockApi.Setup(a =>
                a.UpdateFleetAsync(Guid.Empty, Guid.Empty, Guid.Empty, It.IsAny<FleetUpdateRequest>(), default, default))
            .ReturnsAsync(new Fleet(
                buildConfigurations: new List<BuildConfiguration2>(),
                fleetRegions: new List<FleetRegion1>(),
                name: "test",
                osName: string.Empty,
                servers: new Servers(
                    all: new FleetServerBreakdown(new ServerStatus()),
                    cloud: new FleetServerBreakdown(new ServerStatus()),
                    metal: new FleetServerBreakdown(new ServerStatus())
                )));

        await m_Client!.Update(new FleetId(), "test", new List<BuildConfigurationId>(), new MultiplayConfig.FleetDefinition
        {
            Regions = new Dictionary<string, MultiplayConfig.ScalingDefinition>
            {
                {"North-America", new MultiplayConfig.ScalingDefinition()}
            }
        });

        m_MockApi.Verify(f =>
            f.AddFleetRegionAsync(Guid.Empty, Guid.Empty, Guid.Empty, null, It.IsAny<AddRegionRequest>(), default, default));
    }

    [Test]
    public async Task Update_UpdatesExistingFleets()
    {
        var regionId = Guid.NewGuid();
        m_MockApi!.Setup(a =>
                a.ListTemplateFleetRegionsAsync(Guid.Empty, Guid.Empty, null, default, default))
            .ReturnsAsync(new List<FleetRegionsTemplateListItem>
            {
                new ("North-America", regionId)
            });

        m_MockApi.Setup(a =>
                a.UpdateFleetAsync(Guid.Empty, Guid.Empty, Guid.Empty, It.IsAny<FleetUpdateRequest>(), default, default))
            .ReturnsAsync(new Fleet(
                buildConfigurations: new List<BuildConfiguration2>(),
                fleetRegions: new List<FleetRegion1>
                {
                    new (regionID: regionId, regionName: "North-America")
                },
                name: "test",
                osName: string.Empty,
                servers: new Servers(
                    all: new FleetServerBreakdown(new ServerStatus()),
                    cloud: new FleetServerBreakdown(new ServerStatus()),
                    metal: new FleetServerBreakdown(new ServerStatus())
                )));

        await m_Client!.Update(new FleetId(), "test", new List<BuildConfigurationId>(), new MultiplayConfig.FleetDefinition
        {
            Regions = new Dictionary<string, MultiplayConfig.ScalingDefinition>
            {
                {"North-America", new MultiplayConfig.ScalingDefinition()}
            }
        });

        m_MockApi.Verify(f => f.UpdateFleetRegionAsync(Guid.Empty, Guid.Empty, Guid.Empty, regionId, It.IsAny<UpdateRegionRequest>(), default, default));
    }
}
