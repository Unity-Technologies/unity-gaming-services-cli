using System;
using System.IO;
using System.IO.Abstractions;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.IO;

namespace Unity.Services.Cli.CloudCode.UnitTest.IO;

class CloudCodeFileStreamTests
{
    const string k_TestFilePath = "something.testfile";

    FileSystemStream m_FileStream;
    CloudCodeFileStream m_CloudCodeFileStream;

    class TestFileStream : FileSystemStream
    {
        public TestFileStream(Stream stream, string path, bool isAsync)
            : base(stream, path, isAsync) { }
    }

    public CloudCodeFileStreamTests()
    {
        m_FileStream = new TestFileStream(File.Create(k_TestFilePath), k_TestFilePath, false);
        m_CloudCodeFileStream = new CloudCodeFileStream(m_FileStream);
    }

    [TearDown]
    public void TearDown()
    {
        m_FileStream.Close();
        File.Delete(k_TestFilePath);
    }

    [Test]
    public void ConstructorWorks()
    {
        Assert.AreEqual(m_CloudCodeFileStream.FileStream, m_FileStream);
    }

    [Test]
    public void CloseWorks()
    {
        m_CloudCodeFileStream.Close();

        try
        {
            var fileStream = File.Open(k_TestFilePath, FileMode.Open);
            fileStream.Close();
        }
        catch (IOException)
        {
            Assert.Fail();
        }
    }

    [Test]
    public void OpenFailsWhenCloseNotCalled()
    {
        var ioExceptionThrown = false;
        try
        {
            var fileStream = File.Open(k_TestFilePath, FileMode.Open);
            fileStream.Close();
        }
        catch (IOException)
        {
            ioExceptionThrown = true;
        }

        Assert.IsTrue(ioExceptionThrown);
    }
}
