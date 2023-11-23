using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

class FilesItemMachineOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_FileMachine = new Machine(
            id: ValidMachineId,
            location: ValidFleetName
        );
    }

    Machine? m_FileMachine;

    [Test]
    public void ConstructFilesMachineItemOutputWithValidInput()
    {
        FilesItemMachineOutput output = new(m_FileMachine!);
        Assert.Multiple(
            () =>
            {
                Assert.That(output.Id, Is.EqualTo(m_FileMachine!.Id));
                Assert.That(output.Location, Is.EqualTo(m_FileMachine!.Location));
            }
        );
    }
}
