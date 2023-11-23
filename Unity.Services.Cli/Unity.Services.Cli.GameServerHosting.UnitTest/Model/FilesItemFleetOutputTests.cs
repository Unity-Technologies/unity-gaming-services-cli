using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

class FilesItemFleetOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_FileFleetDetails = new FleetDetails(
            id: new Guid(ValidFleetId),
            name: ValidFleetName
        );
    }

    FleetDetails? m_FileFleetDetails;

    [Test]
    public void ConstructFilesFleetDetailsItemOutputWithValidInput()
    {
        FilesItemFleetOutput output = new(m_FileFleetDetails!);
        Assert.Multiple(
            () =>
            {
                Assert.That(output.Id, Is.EqualTo(m_FileFleetDetails!.Id));
                Assert.That(output.Name, Is.EqualTo(m_FileFleetDetails!.Name));
            }
        );
    }
}
