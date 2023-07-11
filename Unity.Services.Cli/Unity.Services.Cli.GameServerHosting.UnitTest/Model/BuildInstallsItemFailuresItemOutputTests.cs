using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
class BuildInstallsItemFailuresItemOutputTests
{
    [SetUp]
    public void Setup()
    {
        m_Failure = new BuildListInner1FailuresInner(
            123,
            "reason",
            DateTime.Now
        );
    }

    BuildListInner1FailuresInner? m_Failure;

    [Test]
    public void ConstructBuildInstallsItemFailuresItemOutputWithValidFailure()
    {
        BuildInstallsItemFailuresItemOutput output = new(m_Failure!);
        Assert.Multiple(() =>
        {
            Assert.That(output.MachineId, Is.EqualTo(m_Failure?.MachineID));
            Assert.That(output.Reason, Is.EqualTo(m_Failure?.Reason));
            Assert.That(output.Updated, Is.EqualTo(m_Failure?.Updated));
        });
    }

    [Test]
    public void BuildInstallsItemFailuresItemOutputToString()
    {
        var sb = new StringBuilder();
        BuildInstallsItemFailuresItemOutput output = new(m_Failure!);
        sb.AppendLine($"machineId: {output.MachineId}");
        sb.AppendLine($"reason: {output.Reason}");
        sb.AppendLine($"updated: {output.Updated}");
        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
