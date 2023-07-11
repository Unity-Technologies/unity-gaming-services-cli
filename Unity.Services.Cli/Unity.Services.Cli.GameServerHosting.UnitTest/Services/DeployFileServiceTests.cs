using System.IO.Abstractions;
using Moq;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Services;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Services;

public class DeployFileServiceTests
{
    readonly Mock<IFile> m_MockFile = new();
    readonly Mock<IDirectory> m_MockDirectory = new();
    readonly DeployFileService m_Service;

    public DeployFileServiceTests()
    {
        m_Service = new DeployFileService(m_MockFile.Object, m_MockDirectory.Object);
    }

    [SetUp]
    public void SetUp()
    {
        m_MockFile.Reset();
        m_MockDirectory.Reset();
    }

    [Test]
    public void ReadAllTextAsync_ReadsFromFile()
    {
        m_Service.ReadAllTextAsync("test", CancellationToken.None);

        m_MockFile.Verify(f => f.ReadAllTextAsync("test", default), Times.Once);
    }

    [Test]
    public void ListFilesToDeploy_ReturnsExistingFiles()
    {
        m_MockFile.Setup(f => f.Exists("test.gsh")).Returns(true);

        var files = m_Service.ListFilesToDeploy(new List<string> { "test.gsh" }, ".gsh");

        Assert.That(files, Contains.Item("test.gsh"));
    }

    [Test]
    public void ListFilesToDeploy_WhenExtensionIsInvalid_Throws()
    {
        m_MockFile.Setup(f => f.Exists("test.foobar")).Returns(true);

        Assert.Throws<InvalidExtensionException>(() =>
        {
            var _ = m_Service.ListFilesToDeploy(new List<string>
            {
                "test.foobar"
            }, ".gsh").ToList();
        });
    }

    [Test]
    public void ListFilesToDeploy_WhenDirectoryAndFileIsMissing_Throws()
    {
        Assert.Throws<PathNotFoundException>(() =>
        {
            var _ = m_Service.ListFilesToDeploy(new List<string>
            {
                "does_not_exist"
            }, ".gsh").ToList();
        });
    }

    [Test]
    public void ListFilesToDeploy_EnumeratesDirectories()
    {
        m_MockDirectory.Setup(f => f.Exists("foo")).Returns(true);
        m_MockDirectory.Setup(d => d.EnumerateFiles("foo", "*.gsh", SearchOption.AllDirectories))
            .Returns(new List<string> { "test.gsh" });

        var files = m_Service.ListFilesToDeploy(new List<string> { "foo" }, ".gsh");

        Assert.That(files, Contains.Item("test.gsh"));
    }

    [Test]
    public void ListFilesToDeploy_OnEmptyInput_UsesCurrentDirectory()
    {
        Assert.Throws<CliException>(() =>
        {
            var _ = m_Service.ListFilesToDeploy(new List<string>(), ".gsh").ToList();
        });
    }
}
