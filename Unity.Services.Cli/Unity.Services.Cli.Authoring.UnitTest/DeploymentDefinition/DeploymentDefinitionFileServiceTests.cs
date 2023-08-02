using System.IO.Abstractions;
using System.Runtime.Intrinsics.Arm;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Cli.Authoring.UnitTest.Service;

[TestFixture]
class DeploymentDefinitionFileServiceTests
{
    Mock<IFile> m_MockFile;
    Mock<IDirectory> m_MockDirectory;
    Mock<IPath> m_MockPath;
    Mock<IDeploymentDefinitionFactory> m_MockFactory;

    DeploymentDefinitionFileService m_DdefFileService;

    public DeploymentDefinitionFileServiceTests()
    {
        m_MockFile = new Mock<IFile>();
        m_MockDirectory = new Mock<IDirectory>();
        m_MockPath = new Mock<IPath>();
        m_MockFactory = new Mock<IDeploymentDefinitionFactory>();
        m_MockFactory
            .Setup(f => f.CreateDeploymentDefinition(It.IsAny<string>()))
            .Returns((string path) => CreateMockDdef(path).Object);
        m_DdefFileService = new DeploymentDefinitionFileService(
            m_MockFile.Object,
            m_MockDirectory.Object,
            m_MockPath.Object,
            m_MockFactory.Object);
    }

    static Mock<IDeploymentDefinition> CreateMockDdef(string path)
    {
        var mockDdef = new Mock<IDeploymentDefinition>();
        mockDdef
            .SetupGet(d => d.Path)
            .Returns(path);
        mockDdef
            .SetupGet(d => d.Name)
            .Returns(Path.GetFileNameWithoutExtension(path));
        return mockDdef;
    }

    [Test]
    public void GetDeploymentDefinitionsForInput_GetsAllDdef()
    {
        var inputPaths = new[]
        {
            "path/to/folder/file.ext",
            "path/to/folder/A.ddef",
            "path/to/otherFolder"
        };

        SetupFilePathDirectoryForInput(inputPaths);

        SetupDirectoryReturn(
            "path/to/folder",
            ".ddef",
            "path/to/folder/A.ddef", "path/to/folder/subfolder/B.ddef");

        var result = m_DdefFileService.GetDeploymentDefinitionsForInput(inputPaths);

        Assert.AreEqual(1, result.InputDeploymentDefinitions.Count);
        Assert.AreEqual(2, result.AllDeploymentDefinitions.Count);
    }

    void SetupFilePathDirectoryForInput(IEnumerable<string> inputPaths)
    {
        foreach (var inputPath in inputPaths)
        {
            var directoryName = inputPath[..inputPath.LastIndexOf('/')];
            m_MockPath
                .Setup(p => p.GetDirectoryName(inputPath))
                .Returns(directoryName);
            m_MockPath
                .Setup(p => p.GetFullPath(inputPath))
                .Returns(inputPath);
            m_MockPath
                .Setup(p => p.GetFullPath(directoryName))
                .Returns(directoryName);
            m_MockFile
                .Setup(f => f.Exists(inputPath))
                .Returns(true);
            m_MockFile
                .Setup(f => f.Exists(directoryName))
                .Returns(false);
            m_MockDirectory
                .Setup(d => d.Exists(directoryName))
                .Returns(true);
        }
    }

    void SetupDirectoryReturn(string directory, string extension, params string[] returnValueParams)
    {
        foreach (var returnValue in returnValueParams)
        {
            m_MockPath
                .Setup(p => p.GetDirectoryName(returnValue))
                .Returns(returnValue[..returnValue.LastIndexOf('/')]);
        }

        m_MockDirectory
            .Setup(d => d.Exists(directory))
            .Returns(true);
        m_MockDirectory
            .Setup(d => d.GetFiles(directory, $"*{extension}", SearchOption.AllDirectories))
            .Returns(returnValueParams);
    }

    [Test]
    public void GetFilesForDeploymentDefinition_ReturnsAllFiles()
    {
        var ddef = CreateMockDdef("path/to/folder/A.ddef");

        var extensions = new[]
        {
            ".js",
            ".rc",
        };

        SetupFilePathDirectoryForInput(
            new[]
            {
                ddef.Object.Path
            });

        SetupDirectoryReturn("path/to/folder", ".js", "path/to/folder/script.js");
        SetupDirectoryReturn("path/to/folder", ".rc", "path/to/folder/config.rc");

        var files = new List<string>();
        foreach (var extension in extensions)
        {
            files.AddRange(m_DdefFileService.GetFilesForDeploymentDefinition(ddef.Object, extension));
        }

        Assert.AreEqual(2, files.Count);
    }

    [Test]
    public void GetDeploymentDefinitionsForInput_MultipleDdefsInFolder_Throws()
    {
        var inputPaths = new[]
        {
            "path/to/folder/file.ext",
            "path/to/folder/A.ddef"
        };

        SetupFilePathDirectoryForInput(inputPaths);
        SetupDirectoryReturn("path/to/folder", ".ddef", "path/to/folder/A.ddef", "path/to/folder/B.ddef");

        Assert.Throws<MultipleDeploymentDefinitionInDirectoryException>(
            () =>
                m_DdefFileService.GetDeploymentDefinitionsForInput(inputPaths));
    }

    [Test]
    public void GetDeploymentDefinitionsForInput_MultipleDdefsInNestedFolders_DoesNotThrow()
    {
        var inputPaths = new[]
        {
            "path/to/folder/subfolder/B.ddef",
            "path/to/folder/A.ddef"
        };

        SetupFilePathDirectoryForInput(inputPaths);
        SetupDirectoryReturn(
            "path/to/folder",
            ".ddef",
            "path/to/folder/A.ddef", "path/to/folder/subfolder/B.ddef");
        SetupDirectoryReturn("path/to/folder/subfolder", ".ddef", "path/to/folder/subfolder/B.ddef");

        Assert.DoesNotThrow(() =>
            m_DdefFileService.GetDeploymentDefinitionsForInput(inputPaths));
    }
}
