using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
public class MachinesItemSpecOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_MachinesItemSpec = new MachineSpec(
            2,
            ValidMachineCpuSeriesShortname,
            2000,
            ValidMachineCpuType,
            20000000
        );
    }

    MachineSpec? m_MachinesItemSpec;

    [Test]
    public void ConstructMachinesItemSpecOutputWithValidInput()
    {
        MachinesItemSpecOutput output = new(m_MachinesItemSpec!);
        Assert.Multiple(
            () =>
            {
                Assert.That(output.CpuCores, Is.EqualTo(m_MachinesItemSpec!.CpuCores));
                Assert.That(output.CpuShortname, Is.EqualTo(m_MachinesItemSpec!.CpuShortname));
                Assert.That(output.CpuSpeed, Is.EqualTo(m_MachinesItemSpec!.CpuSpeed));
                Assert.That(output.CpuType, Is.EqualTo(m_MachinesItemSpec!.CpuType));
                Assert.That(output.Memory, Is.EqualTo(m_MachinesItemSpec!.Memory));
            }
        );
    }

    [Test]
    public void MachinesItemSpecOutputToString()
    {
        MachinesItemSpecOutput output = new(m_MachinesItemSpec!);
        var sb = new StringBuilder();
        sb.AppendLine($"cpuCores: {m_MachinesItemSpec!.CpuCores}");
        sb.AppendLine($"cpuShortname: {m_MachinesItemSpec!.CpuShortname}");
        sb.AppendLine($"cpuSpeed: {m_MachinesItemSpec!.CpuSpeed}");
        sb.AppendLine($"cpuType: {m_MachinesItemSpec!.CpuType}");
        sb.AppendLine($"memory: {m_MachinesItemSpec!.Memory}");

        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
