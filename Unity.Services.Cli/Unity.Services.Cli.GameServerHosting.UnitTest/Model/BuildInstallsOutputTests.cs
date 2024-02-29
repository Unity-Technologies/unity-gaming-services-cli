using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
class BuildInstallsOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_Installs = new List<BuildListInner1>();

        m_Installs.Add(
            new BuildListInner1(
                ValidBuildVersionName,
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
            )
        );

        m_Installs.Add(
            new BuildListInner1(
                ValidBuildVersionName,
                new CCDDetails(
                    Guid.Parse(ValidBucketId),
                    Guid.Parse(ValidReleaseId)
                ),
                completedMachines: 3,
                container: new ContainerImage(
                    "tag"
                ),
                failures: new List<BuildListInner1FailuresInner>
                {
                    new(
                        3456,
                        "failure",
                        DateTime.Now
                    )
                },
                fleetName: "another fleet name",
                pendingMachines: 2,
                regions: new List<RegionsInner>
                {
                    new(
                        3,
                        1,
                        2,
                        "another region name"
                    )
                }
            )
        );
    }

    List<BuildListInner1>? m_Installs;

    [Test]
    public void ConstructBuildInstallsOutputWithValidInstalls()
    {
        BuildInstallsOutput output = new(m_Installs!);
        Assert.That(output, Has.Count.EqualTo(m_Installs!.Count));
        for (var i = 0; i < output.Count; i++)
            Assert.Multiple(
                () =>
                {
                    Assert.That(output[i].FleetName, Is.EqualTo(m_Installs[i].FleetName));
                    Assert.That(output[i].Ccd?.BucketId, Is.EqualTo(m_Installs[i].Ccd.BucketID));
                    Assert.That(output[i].Ccd?.ReleaseId, Is.EqualTo(m_Installs[i].Ccd.ReleaseID));
                    Assert.That(output[i].Container, Is.EqualTo(m_Installs[i].Container));
                    Assert.That(output[i].PendingMachines, Is.EqualTo(m_Installs[i].PendingMachines));
                    Assert.That(output[i].CompletedMachines, Is.EqualTo(m_Installs[i].CompletedMachines));

                    for (var j = 0; j < output[i].Failures.Count; j++)
                    {
                        Assert.That(
                            output[i].Failures[j].MachineId,
                            Is.EqualTo(m_Installs[i].Failures[j].MachineID));
                        Assert.That(
                            output[i].Failures[j].Reason,
                            Is.EqualTo(m_Installs[i].Failures[j].Reason));
                        Assert.That(
                            output[i].Failures[j].Updated,
                            Is.EqualTo(m_Installs[i].Failures[j].Updated));
                    }

                    for (var j = 0; j < output[i].Regions.Count; j++)
                    {
                        Assert.That(
                            output[i].Regions[j].RegionName,
                            Is.EqualTo(m_Installs[i].Regions[j].RegionName));
                        Assert.That(
                            output[i].Regions[j].PendingMachines,
                            Is.EqualTo(m_Installs[i].Regions[j].PendingMachines));
                        Assert.That(
                            output[i].Regions[j].CompletedMachines,
                            Is.EqualTo(m_Installs[i].Regions[j].CompletedMachines));
                        Assert.That(
                            output[i].Regions[j].Failures,
                            Is.EqualTo(m_Installs[i].Regions[j].Failures));
                    }
                });
    }

    [Test]
    public void BuildInstallsOutputToString()
    {
        BuildInstallsOutput output = new(m_Installs!);
        var sb = new StringBuilder();
        sb.AppendLine("- fleetName: fleet name");
        sb.AppendLine("  ccd:");
        sb.AppendLine("    bucketId: " + ValidBucketId);
        sb.AppendLine("    releaseId: " + ValidReleaseId);
        sb.AppendLine("  container:");
        sb.AppendLine("    imageTag: tag");
        sb.AppendLine("  pendingMachines: 1");
        sb.AppendLine("  completedMachines: 1");
        sb.AppendLine("  failures:");
        sb.AppendLine("  - machineId: 1234");
        sb.AppendLine("    reason: failure");
        sb.AppendLine("    updated: " + m_Installs?[0].Failures[0].Updated);
        sb.AppendLine("  regions:");
        sb.AppendLine("  - regionName: region name");
        sb.AppendLine("    pendingMachines: 1");
        sb.AppendLine("    completedMachines: 1");
        sb.AppendLine("    failures: 1");
        sb.AppendLine("- fleetName: another fleet name");
        sb.AppendLine("  ccd:");
        sb.AppendLine("    bucketId: " + ValidBucketId);
        sb.AppendLine("    releaseId: " + ValidReleaseId);
        sb.AppendLine("  container:");
        sb.AppendLine("    imageTag: tag");
        sb.AppendLine("  pendingMachines: 2");
        sb.AppendLine("  completedMachines: 3");
        sb.AppendLine("  failures:");
        sb.AppendLine("  - machineId: 3456");
        sb.AppendLine("    reason: failure");
        sb.AppendLine("    updated: " + m_Installs?[1].Failures[0].Updated);
        sb.AppendLine("  regions:");
        sb.AppendLine("  - regionName: another region name");
        sb.AppendLine("    pendingMachines: 2");
        sb.AppendLine("    completedMachines: 3");
        sb.AppendLine("    failures: 1");
        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
