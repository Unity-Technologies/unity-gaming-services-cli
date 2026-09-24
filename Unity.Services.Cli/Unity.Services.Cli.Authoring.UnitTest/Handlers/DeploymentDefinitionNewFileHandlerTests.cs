using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;

namespace Unity.Services.Cli.Authoring.UnitTest.Handlers;

[TestFixture]
class DeploymentDefinitionNewFileHandlerTests
{
    MockFileSystem m_FileSystem = null!;
    Mock<ILogger> m_MockLogger = null!;

    [SetUp]
    public void SetUp()
    {
        m_FileSystem = new MockFileSystem();
        m_MockLogger = new Mock<ILogger>();
    }

    [Test]
    public void CreateNewFileCommand_ReturnsCommandNamedNewFile()
    {
        var command = DeploymentDefinitionNewFileHandler.CreateNewFileCommand();
        Assert.That(command.Name, Is.EqualTo("new-file"));
    }

    [Test]
    public void CreateNewFileCommand_HasFileArgument()
    {
        var command = DeploymentDefinitionNewFileHandler.CreateNewFileCommand();
        Assert.That(command.Arguments, Has.Count.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task NewFileAsync_WithNoInput_UsesDefaultFileName()
    {
        var input = new NewFileInput { File = null };

        await DeploymentDefinitionNewFileHandler.NewFileAsync(
            input, m_FileSystem.File, m_FileSystem.Directory, m_FileSystem.Path,
            m_MockLogger.Object, CancellationToken.None);

        var expectedPath = DDefConstants.DefaultFileName + DDefConstants.Extension;
        Assert.That(m_FileSystem.File.Exists(expectedPath), Is.True);

        var json = JObject.Parse(m_FileSystem.File.ReadAllText(expectedPath));
        Assert.That(json["name"]!.Value<string>(), Is.EqualTo(DDefConstants.DefaultFileName));
    }

    [Test]
    public async Task NewFileAsync_WithCustomName_UsesProvidedNameInJson()
    {
        var input = new NewFileInput { File = "my-deployment" };

        await DeploymentDefinitionNewFileHandler.NewFileAsync(
            input, m_FileSystem.File, m_FileSystem.Directory, m_FileSystem.Path,
            m_MockLogger.Object, CancellationToken.None);

        var expectedPath = "my-deployment" + DDefConstants.Extension;
        Assert.That(m_FileSystem.File.Exists(expectedPath), Is.True);

        var json = JObject.Parse(m_FileSystem.File.ReadAllText(expectedPath));
        Assert.That(json["name"]!.Value<string>(), Is.EqualTo("my-deployment"));
    }

    [Test]
    public async Task NewFileAsync_WithDdefExtension_UsesFileNameWithoutExtensionAsName()
    {
        var input = new NewFileInput { File = "my-deployment.ddef" };

        await DeploymentDefinitionNewFileHandler.NewFileAsync(
            input, m_FileSystem.File, m_FileSystem.Directory, m_FileSystem.Path,
            m_MockLogger.Object, CancellationToken.None);

        var expectedPath = "my-deployment" + DDefConstants.Extension;
        Assert.That(m_FileSystem.File.Exists(expectedPath), Is.True);

        var json = JObject.Parse(m_FileSystem.File.ReadAllText(expectedPath));
        Assert.That(json["name"]!.Value<string>(), Is.EqualTo("my-deployment"));
    }

    [Test]
    public async Task NewFileAsync_FileAlreadyExists_DoesNotOverwrite()
    {
        var filePath = "existing" + DDefConstants.Extension;
        m_FileSystem.AddFile(filePath, new MockFileData("original content"));
        var input = new NewFileInput { File = "existing", UseForce = false };

        await DeploymentDefinitionNewFileHandler.NewFileAsync(
            input, m_FileSystem.File, m_FileSystem.Directory, m_FileSystem.Path,
            m_MockLogger.Object, CancellationToken.None);

        Assert.That(m_FileSystem.File.ReadAllText(filePath), Is.EqualTo("original content"));
    }

    [Test]
    public async Task NewFileAsync_FileAlreadyExistsWithForce_Overwrites()
    {
        var filePath = "existing" + DDefConstants.Extension;
        m_FileSystem.AddFile(filePath, new MockFileData("original content"));
        var input = new NewFileInput { File = "existing", UseForce = true };

        await DeploymentDefinitionNewFileHandler.NewFileAsync(
            input, m_FileSystem.File, m_FileSystem.Directory, m_FileSystem.Path,
            m_MockLogger.Object, CancellationToken.None);

        var json = JObject.Parse(m_FileSystem.File.ReadAllText(filePath));
        Assert.That(json["name"]!.Value<string>(), Is.EqualTo("existing"));
    }

    [Test]
    public async Task NewFileAsync_WithSubdirectory_CreatesDirectory()
    {
        var input = new NewFileInput { File = "sub/dir/my-config" };

        await DeploymentDefinitionNewFileHandler.NewFileAsync(
            input, m_FileSystem.File, m_FileSystem.Directory, m_FileSystem.Path,
            m_MockLogger.Object, CancellationToken.None);

        Assert.That(m_FileSystem.Directory.Exists("sub/dir"), Is.True);
        Assert.That(m_FileSystem.File.Exists("sub/dir/my-config" + DDefConstants.Extension), Is.True);
    }
}
