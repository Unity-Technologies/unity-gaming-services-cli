using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using File = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.File;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

class FilesItemOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_File = new File(
            filename: "error.log",
            path: "logs/",
            fileSize: 100,
            createdAt: new DateTime(2022, 10, 11),
            lastModified: new DateTime(2022, 10, 12),
            fleet: new FleetDetails(
                id: new Guid(ValidFleetId),
                name: "Test Fleet"
            ),
            machine: new Machine(
                id: ValidMachineId,
                location: "europe-west1"
            ),
            serverID: ValidServerId
        );
    }

    File? m_File;

    [Test]
    public void ConstructFilesItemOutputWithValidInput()
    {
        FilesItemOutput output = new(m_File!);
        Assert.Multiple(
            () =>
            {
                Assert.That(output.CreatedAt, Is.EqualTo(m_File!.CreatedAt));
                Assert.That(output.FileSize, Is.EqualTo(m_File!.FileSize));
                Assert.That(output.Filename, Is.EqualTo(m_File!.Filename));
                Assert.That(output.Fleet.Id, Is.EqualTo(m_File!.Fleet.Id));
                Assert.That(output.Fleet.Name, Is.EqualTo(m_File!.Fleet.Name));
                Assert.That(output.LastModified, Is.EqualTo(m_File!.LastModified));
                Assert.That(output.Machine.Id, Is.EqualTo(m_File!.Machine.Id));
                Assert.That(output.Machine.Location, Is.EqualTo(m_File!.Machine.Location));
                Assert.That(output.Path, Is.EqualTo(m_File!.Path));
                Assert.That(output.ServerId, Is.EqualTo(m_File!.ServerID));
            }
        );
    }
}
