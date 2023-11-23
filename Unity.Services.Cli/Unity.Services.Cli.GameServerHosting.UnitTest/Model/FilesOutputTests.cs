using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using File = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.File;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
class FilesOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_Files = new List<File>
        {
            new File(
                createdAt: new DateTime(2022, 10, 11),
                fileSize: 100,
                filename: "path/error.log",
                fleet: new FleetDetails(
                    id: new Guid(ValidFleetId),
                    name: ValidFleetName
                ),
                lastModified: new DateTime(2022, 10, 12),
                machine: new Machine(
                    id: ValidMachineId,
                    location: "europe-west1"
                ),
                path: "/games/tf2/",
                serverID: ValidServerId
            )
        };
    }

    List<File>? m_Files;

    [Test]
    public void ConstructFilesOutputWithValidInput()
    {
        FilesOutput output = new(m_Files!);
        for (var i = 0; i < output.Count; i++)
        {
            Assert.Multiple(
                () =>
                {
                    Assert.That(output[i].CreatedAt, Is.EqualTo(m_Files![i].CreatedAt));
                    Assert.That(output[i].FileSize, Is.EqualTo(m_Files![i].FileSize));
                    Assert.That(output[i].Filename, Is.EqualTo(m_Files![i].Filename));
                    Assert.That(output[i].Fleet.Id, Is.EqualTo(m_Files![i].Fleet.Id));
                    Assert.That(output[i].Fleet.Name, Is.EqualTo(m_Files![i].Fleet.Name));
                    Assert.That(output[i].LastModified, Is.EqualTo(m_Files![i].LastModified));
                    Assert.That(output[i].Machine.Id, Is.EqualTo(m_Files![i].Machine.Id));
                    Assert.That(output[i].Machine.Location, Is.EqualTo(m_Files![i].Machine.Location));
                    Assert.That(output[i].Path, Is.EqualTo(m_Files![i].Path));
                    Assert.That(output[i].ServerId, Is.EqualTo(m_Files![i].ServerID));
                }
            );
        }
    }


}
