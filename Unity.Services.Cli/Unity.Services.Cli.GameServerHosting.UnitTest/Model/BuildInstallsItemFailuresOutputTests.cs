using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
class BuildInstallsItemFailuresOutputTests
{
    [SetUp]
    public void Setup()
    {
        m_Failures = new List<BuildListInner1FailuresInner>
        {
            new(
                123,
                "reason",
                DateTime.Now
            ),
            new(
                456,
                "reason2",
                DateTime.Now
            )
        };
    }

    List<BuildListInner1FailuresInner>? m_Failures;

    [Test]
    public void ConstructBuildInstallsItemFailuresOutputWithValidFailures()
    {
        BuildInstallsItemFailuresOutput output = new(m_Failures!);
        Assert.That(output, Has.Count.EqualTo(m_Failures!.Count));
        for (var i = 0; i < output.Count; i++)
            Assert.Multiple(() =>
            {
                Assert.That(output[i].MachineId, Is.EqualTo(m_Failures[i].MachineID));
                Assert.That(output[i].Reason, Is.EqualTo(m_Failures[i].Reason));
                Assert.That(output[i].Updated, Is.EqualTo(m_Failures[i].Updated));
            });
    }

    [Test]
    public void BuildInstallsItemFailuresOutputToString()
    {
        var sb = new StringBuilder();
        BuildInstallsItemFailuresOutput output = new(m_Failures!);
        foreach (var failure in output)
        {
            sb.AppendLine($"- machineId: {failure.MachineId}");
            sb.AppendLine($"  reason: {failure.Reason}");
            sb.AppendLine($"  updated: {failure.Updated}");
        }

        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
