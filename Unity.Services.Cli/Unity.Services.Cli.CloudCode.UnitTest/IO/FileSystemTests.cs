using System.IO.Abstractions;
using Moq;
using NUnit.Framework;
using FileSystem = Unity.Services.Cli.CloudCode.IO.FileSystem;

namespace Unity.Services.Cli.CloudCode.UnitTest.IO;

class FileSystemTests
{
    const string k_TestFilePath = "something.testfile";
    const string k_TestDirectoryPath = "some/directory/path";

    Mock<IFile> m_MockFile;
    Mock<IPath> m_MockPath;
    Mock<IDirectory> m_MockDirectory;
    FileSystem m_FileSystem;

    public FileSystemTests()
    {
        m_MockFile = new Mock<IFile>();
        m_MockPath = new Mock<IPath>();
        m_MockDirectory = new Mock<IDirectory>();
        m_FileSystem = new FileSystem(
            m_MockFile.Object,
            m_MockPath.Object,
            m_MockDirectory.Object);
    }

    [Test]
    public void CreateFile()
    {
        m_FileSystem.CreateFile(k_TestFilePath);
        m_MockFile.Verify(f => f.Create(k_TestFilePath), Times.Once);
    }

    [Test]
    public void FileExists()
    {
        m_FileSystem.FileExists(k_TestFilePath);
        m_MockFile.Verify(f => f.Exists(k_TestFilePath), Times.Once);
    }

    [Test]
    public void DirectoryExists()
    {
        m_FileSystem.DirectoryExists(k_TestDirectoryPath);
        m_MockDirectory.Verify(d => d.Exists(k_TestDirectoryPath));
    }

    [Test]
    public void GetDirectoryName()
    {
        m_FileSystem.GetDirectoryName(k_TestFilePath);
        m_MockPath.Verify(p => p.GetDirectoryName(k_TestFilePath), Times.Once);
    }

    [Test]
    public void Combine()
    {
        var paths = new[]
        {
            "1",
            "2",
            "3"
        };
        m_FileSystem.Combine(paths);
        m_MockPath.Verify(p => p.Combine(paths), Times.Once);
    }
}
