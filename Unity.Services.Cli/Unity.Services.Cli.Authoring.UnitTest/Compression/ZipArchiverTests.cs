using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Compression;

namespace Unity.Services.Cli.Authoring.UnitTest.Compression;

[TestFixture]
public class ZipArchiverTests
{
    ZipArchiver<string> m_Archiver = new();

    IReadOnlyList<string> m_Data = new[]
    {
        "Zip",
        "Unzip",
        "Check"
    };

    const string k_Extension = "test-zip";
    const string k_Path = "test";
    const string k_EntryName = "__ugs-cli";
    const string k_DirectoryName = "test-zip";

    [SetUp]
    public void SetUp()
    {
        if (Directory.Exists(k_Path))
        {
            Directory.Delete(k_Path, true);
        }
        Directory.CreateDirectory(k_Path);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(k_Path))
        {
            Directory.Delete(k_Path, true);
        }
    }

    [Test]
    public void TestZipAndUnZipDataAreEqual()
    {
        m_Archiver.Zip(k_Path, k_DirectoryName, k_EntryName, k_Extension, m_Data);

        var unZipData = m_Archiver.Unzip(k_Path, k_EntryName, k_Extension);

        Assert.AreEqual(m_Data, unZipData);
    }
}
