using System.Text;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
class BuildConfigurationOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_BuildConfiguration = new BuildConfiguration(
            binaryPath: "/path/to/simple-go-server",
            buildID: long.Parse(ValidBuildId),
            buildName: ValidBuildName,
            commandLine: "simple-go-server",
            configuration: new List<ConfigEntry>()
            {
                new ConfigEntry(
                    id: 0,
                    key: "key",
                    value: "value"
                ),
            },
            cores: 2L,
            createdAt: new DateTime(2022, 10, 11),
            fleetID: new Guid(ValidFleetId),
            fleetName: ValidFleetName,
            id: ValidBuildConfigurationId,
            memory: 800L,
            name: ValidBuildConfigurationName,
            queryType: "sqp",
            speed: 1200L,
            updatedAt: new DateTime(2022, 10, 11),
            version: 1L
        );
    }

    BuildConfiguration? m_BuildConfiguration;

    [Test]
    public void ConstructBuildConfigurationOutputWithValidBuildConfiguration()
    {
        BuildConfigurationOutput output = new(m_BuildConfiguration!);
        Assert.Multiple(() =>
        {
            Assert.That(output.BuildId, Is.EqualTo(m_BuildConfiguration!.BuildID));
            Assert.That(output.BuildName, Is.EqualTo(m_BuildConfiguration!.BuildName));
            Assert.That(output.CommandLine, Is.EqualTo(m_BuildConfiguration!.CommandLine));
            Assert.That(output.Cores, Is.EqualTo(m_BuildConfiguration!.Cores));
            Assert.That(output.CreatedAt, Is.EqualTo(m_BuildConfiguration!.CreatedAt));
            Assert.That(output.FleetId, Is.EqualTo(m_BuildConfiguration!.FleetID));
            Assert.That(output.FleetName, Is.EqualTo(m_BuildConfiguration!.FleetName));
            Assert.That(output.Id, Is.EqualTo(m_BuildConfiguration!.Id));
            Assert.That(output.Memory, Is.EqualTo(m_BuildConfiguration!.Memory));
            Assert.That(output.Name, Is.EqualTo(m_BuildConfiguration!.Name));
            Assert.That(output.QueryType, Is.EqualTo(m_BuildConfiguration!.QueryType));
            Assert.That(output.Speed, Is.EqualTo(m_BuildConfiguration!.Speed));
            Assert.That(output.UpdatedAt, Is.EqualTo(m_BuildConfiguration!.UpdatedAt));
            Assert.That(output.Version, Is.EqualTo(m_BuildConfiguration!._Version));

            for (var i = 0; i < m_BuildConfiguration!._Configuration.Count; i++)
            {
                Assert.That(output.Configuration[i].Id, Is.EqualTo(m_BuildConfiguration!._Configuration[i].Id));
                Assert.That(output.Configuration[i].Key, Is.EqualTo(m_BuildConfiguration!._Configuration[i].Key));
                Assert.That(output.Configuration[i].Value, Is.EqualTo(m_BuildConfiguration!._Configuration[i].Value));
            }
        });
    }
}
