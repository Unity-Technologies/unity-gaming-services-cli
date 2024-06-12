using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Core = Unity.Services.Matchmaker.Authoring.Core.Model;

namespace Unity.Services.Cli.Matchmaker.UnitTest.SampleConfigs;

class MultiplaySampleConfig
{
    public Core.MultiplayResources LocalResources = new Core.MultiplayResources()
    {
        Fleets = new List<Core.MultiplayResources.Fleet>()
        {
            new Core.MultiplayResources.Fleet()
            {
                Name = "TestFleet",
                Id = "e8b109e1-6746-4ce6-9c21-3330509554a1",
                BuildConfigs = new List<Core.MultiplayResources.Fleet.BuildConfig>()
                {
                    new Core.MultiplayResources.Fleet.BuildConfig()
                    {
                        Name = "TestBuildConfig",
                        Id = "74874928923749"
                    }
                },
                QosRegions = new List<Core.MultiplayResources.Fleet.QosRegion>()
                {
                    new Core.MultiplayResources.Fleet.QosRegion()
                    {
                        Name = "NorthAmerica",
                        Id = "3eac13c4-bf61-4b05-83df-eed5732ad305"
                    }
                }
            }
        }
    };

    public MultiplayConfig LocalConfigs = new MultiplayConfig()
    {
        Fleets = new Dictionary<FleetName, MultiplayConfig.FleetDefinition>()
        {
            {
                new FleetName() { Name = "TestFleet" }, new MultiplayConfig.FleetDefinition()
                {
                    BuildConfigurations = new List<BuildConfigurationName>()
                        { new BuildConfigurationName() { Name = "TestBuildConfig" } },
                    Regions = new Dictionary<string, MultiplayConfig.ScalingDefinition>()
                    {
                        { "NorthAmerica", new MultiplayConfig.ScalingDefinition() }
                    }
                }
            }
        }
    };

    public List<FleetListItem> RemoteFleets = new List<FleetListItem>()
    {
        new FleetListItem(
            name: "TestFleet",
            id: Guid.Parse("e8b109e1-6746-4ce6-9c21-3330509554a1"),
            osName: "Windows",
            osID: Guid.Parse("f84109e1-1746-4ce6-9f21-3330509554a1"),
            servers: new Servers(
                all: new FleetServerBreakdown(status: new ServerStatus()),
                cloud: new FleetServerBreakdown(status: new ServerStatus()),
                metal: new FleetServerBreakdown(status: new ServerStatus())),
            buildConfigurations: new List<BuildConfiguration1>()
            {
                new BuildConfiguration1(
                    name: "TestBuildConfig",
                    id: 74874928923749
                )
            },
            regions: new List<FleetRegion>()
            {
                new FleetRegion(
                    regionName: "NorthAmerica",
                    regionID: Guid.Parse("3eac13c4-bf61-4b05-83df-eed5732ad305")
                )
            }
        )
    };
}
