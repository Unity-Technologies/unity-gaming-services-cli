using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Deploy.Exceptions;
using Unity.Services.Cli.Deploy.Service;

namespace Unity.Services.Cli.Deploy.UnitTest.Service;

[TestFixture]
public class DeployFileServiceTests
{
    readonly Mock<IFile> m_MockFile = new();
    readonly Mock<IDirectory> m_MockDirectory = new();
    readonly Mock<IPath> m_MockPath = new();
    readonly DeployFileService m_Service;

    public DeployFileServiceTests()
    {
        m_Service = new DeployFileService(m_MockFile.Object, m_MockDirectory.Object, m_MockPath.Object);
    }

    [SetUp]
    public void SetUp()
    {
        m_MockFile.Reset();
        m_MockDirectory.Reset();
        m_MockPath.Reset();
    }

    [Test]
    public void ListFilesToDeployReturnsExistingFiles()
    {
        m_MockFile.Setup(f => f.Exists("test.mps")).Returns(true);
        m_MockPath.Setup(p => p.GetFullPath("test.mps")).Returns("test.mps");
        var files = m_Service.ListFilesToDeploy(new List<string>
        {
            "test.mps"
        }, ".mps");

        Assert.That(files, Contains.Item("test.mps"));
    }

    [Test]
    public void ListFilesToDeployWhenDirectoryAndFileIsMissingThrowPathNotFoundException()
    {
        Assert.Throws<PathNotFoundException>(() =>
        {
            var _ = m_Service.ListFilesToDeploy(new List<string>
            {
                "does_not_exist"
            }, ".mps").ToList();
        });
    }
    [Test]
    public void ListFilesToDeployThrowExceptionForDirectoryWithoutAccessPermission()
    {
        m_MockPath.Setup(p => p.GetFullPath("foo")).Returns("foo");
        m_MockDirectory.Setup(f => f.Exists("foo")).Returns(true);
        m_MockDirectory.Setup(d => d.GetFiles("foo", "*.mps", SearchOption.AllDirectories))
            .Throws<UnauthorizedAccessException>();

        Assert.Throws<CliException>(() => m_Service.ListFilesToDeploy(new List<string>
        {
            "foo"
        }, ".mps"));
    }

    [Test]
    public void ListFilesToDeployEnumeratesDirectories()
    {
        m_MockPath.Setup(p => p.GetFullPath("foo")).Returns("foo");
        m_MockDirectory.Setup(f => f.Exists("foo")).Returns(true);
        m_MockDirectory.Setup(d => d.GetFiles("foo", "*.mps", SearchOption.AllDirectories))
            .Returns(new[]
            {
                "test.mps"
            });

        var files = m_Service.ListFilesToDeploy(new List<string>
        {
            "foo"
        }, ".mps");

        Assert.That(files, Contains.Item("test.mps"));
    }

    [Test]
    public void ListFilesToDeployEnumeratesDirectoriesSorted()
    {
        var expectedFiles = new List<string>
        {
            "c.mps",
            "b.mps",
            "a.mps"
        };
        m_MockPath.Setup(p => p.GetFullPath("foo")).Returns("foo");
        m_MockDirectory.Setup(f => f.Exists("foo")).Returns(true);
        m_MockDirectory.Setup(d => d.GetFiles("foo", "*.mps", SearchOption.AllDirectories))
            .Returns(expectedFiles.ToArray);
        var files = m_Service.ListFilesToDeploy(new List<string>
        {
            "foo"
        }, ".mps");
        expectedFiles.Sort();
        CollectionAssert.AreEqual(expectedFiles, files);
    }

    [Test]
    public void ListFilesToDeployEnumeratesDirectoriesRemoveDuplicate()
    {
        var expectedFiles = new List<string>
        {
            "c.mps",
            "b.mps",
            "a.mps",
            "c.mps"
        };
        m_MockPath.Setup(p => p.GetFullPath("foo")).Returns("foo");
        m_MockDirectory.Setup(f => f.Exists("foo")).Returns(true);
        m_MockDirectory.Setup(d => d.GetFiles("foo", "*.mps", SearchOption.AllDirectories))
            .Returns(expectedFiles.ToArray);
        var files = m_Service.ListFilesToDeploy(new List<string>
        {
            "foo"
        }, ".mps");
        expectedFiles = expectedFiles.Distinct().ToList();
        expectedFiles.Sort();
        CollectionAssert.AreEqual(expectedFiles, files);
    }

    [Test]
    public void ListFilesToDeployOnEmptyInputThrowDeployException()
    {
        Assert.Throws<DeployException>(() => m_Service.ListFilesToDeploy(new List<string>(), ".mps"));
    }

    [Test]
    public async Task LoadContentAsyncSuccessful()
    {
        m_MockFile.Setup(f => f.Exists("foo")).Returns(true);
        m_MockFile.Setup(f => f.ReadAllTextAsync("foo", CancellationToken.None)).ReturnsAsync("{}");
        var content = await m_Service.LoadContentAsync("foo", CancellationToken.None);
        Assert.AreEqual("{}", content);
    }

    [Test]
    public void LoadContentAsyncFailedWithFileNotFound()
    {
        m_MockFile.Setup(f => f.Exists("foo")).Returns(true);
        m_MockFile.Setup(f => f.ReadAllTextAsync("foo", CancellationToken.None))
            .ThrowsAsync(new FileNotFoundException());

         Assert.ThrowsAsync<CliException>(async() => await m_Service.LoadContentAsync("foo", CancellationToken.None));
    }

    [Test]
    public void LoadContentAsyncFailedWithUnauthorizedAccess()
    {
        m_MockFile.Setup(f => f.Exists("foo")).Returns(true);
        m_MockFile.Setup(f => f.ReadAllTextAsync("foo", CancellationToken.None))
            .ThrowsAsync(new UnauthorizedAccessException());

        Assert.ThrowsAsync<CliException>(async() => await m_Service.LoadContentAsync("foo", CancellationToken.None));
    }

    [Test]
    public void LoadContentAsyncFailedWithUnexpectedException()
    {
        m_MockFile.Setup(f => f.Exists("foo")).Returns(true);
        m_MockFile.Setup(f => f.ReadAllTextAsync("foo", CancellationToken.None))
            .ThrowsAsync(new Exception());

        Assert.ThrowsAsync<Exception>(async() => await m_Service.LoadContentAsync("foo", CancellationToken.None));
    }
}
