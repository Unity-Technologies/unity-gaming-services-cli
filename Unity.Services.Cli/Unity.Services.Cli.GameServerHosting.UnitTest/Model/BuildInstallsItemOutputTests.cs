using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
class BuildInstallsItemOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_Install = new BuildListInner1(
            new CCDDetails(
                Guid.Parse(ValidBucketId),
                Guid.Parse(ValidReleaseId)
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
        );
    }

    BuildListInner1? m_Install;

    [Test]
    public void ConstructBuildInstallsItemOutputWithValidInstalls()
    {
        BuildInstallsItemOutput output = new(m_Install!);
        Assert.Multiple(() =>
        {
            Assert.That(output.FleetName, Is.EqualTo(m_Install!.FleetName));
            Assert.That(output.Ccd?.BucketId, Is.EqualTo(m_Install!.Ccd.BucketID));
            Assert.That(output.Ccd?.ReleaseId, Is.EqualTo(m_Install!.Ccd.ReleaseID));
            Assert.That(output.Container, Is.EqualTo(m_Install!.Container));
            Assert.That(output.PendingMachines, Is.EqualTo(m_Install!.PendingMachines));
            Assert.That(output.CompletedMachines, Is.EqualTo(m_Install!.CompletedMachines));

            for (var i = 0; i < m_Install!.Failures.Count; i++)
            {
                Assert.That(output.Failures[i].MachineId, Is.EqualTo(m_Install!.Failures[i].MachineID));
                Assert.That(output.Failures[i].Reason, Is.EqualTo(m_Install!.Failures[i].Reason));
                Assert.That(output.Failures[i].Updated, Is.EqualTo(m_Install!.Failures[i].Updated));
            }

            for (var i = 0; i < m_Install!.Regions.Count; i++)
            {
                Assert.That(output.Regions[i].RegionName, Is.EqualTo(m_Install!.Regions[i].RegionName));
                Assert.That(output.Regions[i].PendingMachines, Is.EqualTo(m_Install!.Regions[i].PendingMachines));
                Assert.That(output.Regions[i].CompletedMachines, Is.EqualTo(m_Install!.Regions[i].CompletedMachines));
                Assert.That(output.Regions[i].Failures, Is.EqualTo(m_Install!.Regions[i].Failures));
            }
        });
    }

    [Test]
    public void BuildInstallsItemOutputToString()
    {
        BuildInstallsItemOutput output = new(m_Install!);
        var sb = new StringBuilder();
        sb.AppendLine("fleetName: fleet name");
        sb.AppendLine("ccd:");
        sb.AppendLine("  bucketId: " + ValidBucketId);
        sb.AppendLine("  releaseId: " + ValidReleaseId);
        sb.AppendLine("container:");
        sb.AppendLine("  imageTag: tag");
        sb.AppendLine("pendingMachines: 1");
        sb.AppendLine("completedMachines: 1");
        sb.AppendLine("failures:");
        sb.AppendLine("- machineId: 1234");
        sb.AppendLine("  reason: failure");
        sb.AppendLine("  updated: " + m_Install!.Failures[0].Updated);
        sb.AppendLine("regions:");
        sb.AppendLine("- regionName: region name");
        sb.AppendLine("  pendingMachines: 1");
        sb.AppendLine("  completedMachines: 1");
        sb.AppendLine("  failures: 1");
        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
