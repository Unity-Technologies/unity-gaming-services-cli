using System.IO.Abstractions;
using Moq;
using Unity.Services.Cli.GameServerHosting.Services;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Services;

public class FileReaderAdapterTests
{
    readonly Mock<IFile> m_MockFiles = new();
    readonly Mock<IDirectory> m_MockDirectory = new();

    [Test]
    public void EnumerateDirectories_CallsEnumerateDirectories()
    {
        new FileReaderAdapter(m_MockFiles.Object, m_MockDirectory.Object).EnumerateDirectories("test");

        m_MockDirectory.Verify(m => m.EnumerateDirectories("test"), Times.Once);
    }

    [Test]
    public void EnumerateFiles_CallsEnumerateFiles()
    {
        new FileReaderAdapter(m_MockFiles.Object, m_MockDirectory.Object).EnumerateFiles("test");

        m_MockDirectory.Verify(m => m.EnumerateFiles("test"), Times.Once);
    }

    [Test]
    public void OpenReadFile_CallsOpenReadFile()
    {
        new FileReaderAdapter(m_MockFiles.Object, m_MockDirectory.Object).OpenReadFile("test");

        m_MockFiles.Verify(m => m.OpenRead("test"), Times.Once);
    }
}
