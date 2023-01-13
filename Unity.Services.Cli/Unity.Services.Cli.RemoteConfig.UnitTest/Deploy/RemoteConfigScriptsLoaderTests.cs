using NUnit.Framework;
using Unity.Services.Cli.Deploy.Model;
using Unity.Services.Cli.RemoteConfig.Deploy;

namespace Unity.Services.Cli.RemoteConfig.UnitTest.Deploy;

[TestFixture]
public class RemoteConfigScriptsLoaderTests
{
    const string k_ValidTempFilePath = @".\validTestTemp.rc";
    const string k_ValidFileContent = "{\"id\":{\"type\":\"string\"},\"entries\":{\"type\":\"string\",\"description\":\"Values to store\"}}";

    const string k_InvalidTempFilePath = @".\invalidTestTemp.rc";
    const string k_InvalidFileContent = "{\"description\":\"Values to store\"}}";

    RemoteConfigScriptsLoader m_RemoteConfigScriptsLoader = new();

    [SetUp]
    public async Task SetUp()
    {
        if (File.Exists(k_ValidTempFilePath))
        {
            File.Delete(k_ValidTempFilePath);
        }
        if (File.Exists(k_InvalidTempFilePath))
        {
            File.Delete(k_InvalidTempFilePath);
        }
        await File.WriteAllTextAsync(k_ValidTempFilePath, k_ValidFileContent);
        await File.WriteAllTextAsync(k_InvalidTempFilePath, k_InvalidFileContent);
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
        var deployContentList = new List<DeployContent>();
        var remoteConfigFiles = await m_RemoteConfigScriptsLoader.LoadScriptsAsync(filepaths, deployContentList);
        Assert.That(remoteConfigFiles, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task LoadScriptsAsync_CorrectlyCreatesLoadedDeployContent()
    {
        var filepaths = new List<string>
        {
            k_ValidTempFilePath
        };
        var deployContentList = new List<DeployContent>();
        await m_RemoteConfigScriptsLoader.LoadScriptsAsync(filepaths, deployContentList);

        Assert.That(deployContentList, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task LoadScriptsAsync_CorrectlyCreatesFailedDeployContent()
    {
        var filepaths = new List<string>
        {
            k_InvalidTempFilePath
        };
        var deployContentList = new List<DeployContent>();
        await m_RemoteConfigScriptsLoader.LoadScriptsAsync(filepaths, deployContentList);

        Assert.That(deployContentList, Has.Count.EqualTo(1));
    }
}
