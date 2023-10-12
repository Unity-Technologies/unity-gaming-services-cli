using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
public class MachinesItemServerStatsOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_ServersStates = new ServersStates(
            allocated: 1,
            available: 2,
            online: 3,
            reserved: 4,
            held: 5
        );
    }

    ServersStates? m_ServersStates;

    [Test]
    public void ConstructMachinesItemServerStatsOutputWithValidInput()
    {
        MachinesItemServerStatsOutput output = new(m_ServersStates!);
        Assert.Multiple(
            () =>
            {
                Assert.That(output.Allocated, Is.EqualTo(m_ServersStates!.Allocated));
                Assert.That(output.Available, Is.EqualTo(m_ServersStates!.Available));
                Assert.That(output.Held, Is.EqualTo(m_ServersStates!.Held));
                Assert.That(output.Online, Is.EqualTo(m_ServersStates!.Online));
                Assert.That(output.Reserved, Is.EqualTo(m_ServersStates!.Reserved));
            }
        );
    }

    [Test]
    public void MachinesItemServerStatsOutputToString()
    {
        MachinesItemServerStatsOutput output = new(m_ServersStates!);
        var sb = new StringBuilder();
        sb.AppendLine($"allocated: {m_ServersStates!.Allocated}");
        sb.AppendLine($"available: {m_ServersStates!.Available}");
        sb.AppendLine($"held: {m_ServersStates!.Held}");
        sb.AppendLine($"online: {m_ServersStates!.Online}");
        sb.AppendLine($"reserved: {m_ServersStates!.Reserved}");

        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
