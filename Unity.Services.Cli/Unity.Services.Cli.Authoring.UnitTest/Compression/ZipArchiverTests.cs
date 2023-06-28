using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.Authoring.UnitTest.Compression;

[TestFixture]
public class ZipArchiverTests
{
    IZipArchiver m_Archiver = new ZipArchiver();

    IReadOnlyList<string> m_Data = new[]
    {
        "Zip",
        "Unzip",
        "Check"
    };

    const string k_Path = "testdata/test.test-zip";

    static string DirectoryName => Path.GetDirectoryName(k_Path)!;

    [SetUp]
    public void SetUp()
    {
        if (Directory.Exists(DirectoryName))
        {
            Directory.Delete(DirectoryName, true);
        }
        Directory.CreateDirectory(DirectoryName);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(DirectoryName))
        {
            Directory.Delete(DirectoryName, true);
        }
    }

    [Test]
    public async Task ZipAndUnzipDataAreEqual()
    {
        var entryName = "test";

        await m_Archiver.ZipAsync(k_Path, entryName, m_Data);
        var unZipData = await m_Archiver.UnzipAsync<string>(k_Path, entryName);

        Assert.AreEqual(m_Data, unZipData);
    }

    [Test]
    public async Task ZipAndUnzipEntryWithDirectoryDataAreEqual()
    {
        var entryName = "testdir/testentry";

        await m_Archiver.ZipAsync(k_Path, entryName, m_Data);
        var unZipData = await m_Archiver.UnzipAsync<string>(k_Path, entryName);

        Assert.AreEqual(m_Data, unZipData);
    }

    [Test]
    public async Task UnzipNoEntryThrows()
    {
        await m_Archiver.ZipAsync(k_Path, "notUsed", m_Data);
        Assert.ThrowsAsync<CliException>(() => m_Archiver.UnzipAsync<string>(k_Path, "nonexistentEntry"));
    }
}
