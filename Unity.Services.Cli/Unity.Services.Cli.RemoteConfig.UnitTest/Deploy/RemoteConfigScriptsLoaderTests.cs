using System.IO.Abstractions;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.RemoteConfig.Deploy;

namespace Unity.Services.Cli.RemoteConfig.UnitTest.Deploy;

[TestFixture]
public class RemoteConfigScriptsLoaderTests
{
    const string k_ValidTempFilePath = "validTestTemp.rc";
    const string k_ValidFileContent = "{\"id\":{\"type\":\"string\"},\"entries\":{\"type\":\"string\",\"description\":\"Values to store\"}}";

    const string k_InvalidTempFilePath = "invalidTestTemp.rc";
    const string k_InvalidFileContent = "{\"description\":\"Values to store\"}}";

    const string k_EmptyTempFilePath = "emptyTemp.rc";
    const string k_EmptyFileContent = "";

    static readonly Mock<IFile> k_File = new ();
    static readonly Mock<IPath> k_Path = new ();

    RemoteConfigScriptsLoader m_RemoteConfigScriptsLoader = new(k_File.Object, k_Path.Object);

    [SetUp]
    public void SetUp()
    {
        k_File.Reset();
        k_File.Setup(f => f.ReadAllTextAsync(k_ValidTempFilePath, CancellationToken.None))
            .ReturnsAsync(k_ValidFileContent);
        k_File.Setup(f => f.ReadAllTextAsync(k_InvalidTempFilePath, CancellationToken.None))
            .ReturnsAsync(k_InvalidFileContent);
        k_File.Setup(f => f.ReadAllTextAsync(k_EmptyTempFilePath, CancellationToken.None))
            .ReturnsAsync(k_EmptyFileContent);
        k_Path.Reset();
        k_Path.Setup(p => p.GetFileName(k_ValidTempFilePath)).Returns(k_ValidTempFilePath);
        k_Path.Setup(p => p.GetFileName(k_InvalidTempFilePath)).Returns(k_InvalidTempFilePath);
        k_Path.Setup(p => p.GetFileName(k_EmptyTempFilePath)).Returns(k_EmptyTempFilePath);

    }

    [TearDown]
    public void TearDown()
    {
        File.Delete(k_ValidTempFilePath);
        File.Delete(k_InvalidTempFilePath);
    }

    [Test]
    public async Task LoadScriptsAsync_ReturnsCorrectRemoteConfigFiles()
    {
        var filepaths = new List<string>
        {
            k_ValidTempFilePath
        };
        var loadResult = await m_RemoteConfigScriptsLoader.LoadScriptsAsync(filepaths, CancellationToken.None);
        Assert.That(loadResult.Loaded.ToList(), Has.Count.EqualTo(1));
        Assert.That(loadResult.Failed.ToList(), Has.Count.EqualTo(0));
    }

    [Test]
    public async Task LoadScriptsAsync_CorrectlyCreatesLoadedDeployContent()
    {
        var filepaths = new List<string>
        {
            k_ValidTempFilePath
        };

        var res = await m_RemoteConfigScriptsLoader.LoadScriptsAsync(filepaths, CancellationToken.None);

        Assert.That(res.Loaded, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task LoadScriptsAsync_InvalidContentCreatesFailedDeployContent()
    {
        var filepaths = new List<string>
        {
            k_InvalidTempFilePath
        };
        var res = await m_RemoteConfigScriptsLoader.LoadScriptsAsync(filepaths, CancellationToken.None);

        Assert.That(res.Failed, Has.Count.EqualTo(1));
        StringAssert.Contains("Additional text encountered after finished reading JSON content: }. Path '', line 1, position 33.",res.Failed.First().Detail);
    }

    [Test]
    public async Task LoadScriptsAsync_EmptyContentCreatesFailedDeployContent()
    {
        var filepaths = new List<string>
        {
            k_EmptyTempFilePath
        };

        var res = await m_RemoteConfigScriptsLoader.LoadScriptsAsync(filepaths, CancellationToken.None);

        Assert.That(res.Failed, Has.Count.EqualTo(1));
        StringAssert.Contains($"{k_EmptyTempFilePath} is not a valid resource",res.Failed.First().Detail);

    }
}
