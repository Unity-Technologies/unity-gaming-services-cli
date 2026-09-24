using System.IO.Abstractions;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Lobby.Deploy;
using Unity.Services.Cli.Lobby.Handlers;
using Unity.Services.Cli.Lobby.Handlers.Config;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Lobby.UnitTest.Deploy;

[TestFixture]
class LobbyFetchServiceTests
{
    readonly Mock<IRemoteConfigService> m_MockRemoteConfigService = new();
    readonly Mock<IFileSystem> m_MockFileSystem = new();

    LobbyFetchHandler m_Handler = null!;
    LobbyFetchService m_Service = null!;

    [SetUp]
    public void SetUp()
    {
        m_MockRemoteConfigService.Reset();
        m_MockFileSystem.Reset();

        var mockFile = new Mock<IFile>();
        mockFile.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        m_MockFileSystem.Setup(fs => fs.File).Returns(mockFile.Object);

        var mockDir = new Mock<IDirectory>();
        m_MockFileSystem.Setup(fs => fs.Directory).Returns(mockDir.Object);

        m_Handler = new LobbyFetchHandler(
            m_MockRemoteConfigService.Object,
            m_MockFileSystem.Object);
        m_Service = new LobbyFetchService(m_Handler);
    }

    [Test]
    public async Task FetchAsync_WhenNoRemoteConfig_ReturnsEmpty()
    {
        SetupNoRemote();

        var result = await m_Service.FetchAsync(
            MakeInput(),
            new[] { new AuthoringFile("dir/lobby.lo") },
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Fetched, Is.Empty);
    }

    [Test]
    public async Task FetchAsync_WhenLocalFileExists_UpdatesFile()
    {
        SetupRemoteConfig();

        var result = await m_Service.FetchAsync(
            MakeInput(),
            new[] { new AuthoringFile("dir/lobby.lo") },
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Updated, Has.Count.EqualTo(1));
        Assert.That(result.Created, Is.Empty);
    }

    [Test]
    public async Task FetchAsync_DryRun_DoesNotWriteFile()
    {
        SetupRemoteConfig();

        var result = await m_Service.FetchAsync(
            MakeInput(dryRun: true),
            new[] { new AuthoringFile("dir/lobby.lo") },
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Fetched, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task FetchAsync_Reconcile_WhenNoLocalFile_CreatesFileAtDefaultPath()
    {
        SetupRemoteConfig();

        var result = await m_Service.FetchAsync(
            MakeInput(reconcile: true),
            Array.Empty<AuthoringFile>(),
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Created, Has.Count.EqualTo(1));
        Assert.That(result.Updated, Is.Empty);
    }

    [Test]
    public async Task FetchAsync_NoReconcile_WhenNoLocalFile_ReturnsEmpty()
    {
        SetupRemoteConfig();

        var result = await m_Service.FetchAsync(
            MakeInput(),
            Array.Empty<AuthoringFile>(),
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Fetched, Is.Empty);
    }

    [Test]
    public async Task FetchAsync_WriteFailure_ItemIsInFailed()
    {
        SetupRemoteConfig();
        RebuildWithFileMock(mockFile =>
            mockFile.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new IOException("disk full")));

        var result = await m_Service.FetchAsync(
            MakeInput(),
            new[] { new AuthoringFile("dir/lobby.lo") },
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Failed, Has.Count.EqualTo(1));
        Assert.That(result.Fetched, Is.Empty);
    }

    [TestCase("lobby")]
    [TestCase("lobbyv2")]
    [TestCase("lobbyv3")]
    public async Task FetchAsync_PreservesSchemaId_AcrossRoundTrip(string schemaId)
    {
        SetupRemoteConfigWithSchemaId(schemaId);
        string? writtenContent = null;
        RebuildWithFileMock(mockFile =>
            mockFile.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((_, content, _) => writtenContent = content)
                .Returns(Task.CompletedTask));

        await m_Service.FetchAsync(
            MakeInput(),
            new[] { new AuthoringFile("dir/lobby.lo") },
            "proj", "env", null, CancellationToken.None);

        Assert.That(writtenContent, Is.Not.Null);
        var parsed = JObject.Parse(writtenContent!);
        Assert.That(parsed["$schema"]?.ToString(), Is.EqualTo(LobbyConfigFile.k_SchemaUrl));
        Assert.That(parsed["schemaId"]?.ToString(), Is.EqualTo(schemaId));
    }

    [Test]
    public async Task FetchAsync_MultipleLoFiles_OnlyFirstFetched_ExtrasFailed()
    {
        SetupRemoteConfig();

        var files = new[]
        {
            new AuthoringFile("dir/lobby.lo"),
            new AuthoringFile("dir/other.lo"),
            new AuthoringFile("dir/third.lo"),
        };

        var result = await m_Service.FetchAsync(
            MakeInput(),
            files,
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Fetched, Has.Count.EqualTo(1));
        Assert.That(result.Fetched[0].Path, Is.EqualTo("dir/lobby.lo"));
        Assert.That(result.Failed, Has.Count.EqualTo(2));
        Assert.That(result.Failed.All(f => f.Status.MessageSeverity == SeverityLevel.Error), Is.True);
        Assert.That(result.Failed.All(f => f.Status.MessageDetail!.Contains("Only one")), Is.True);
    }

    [Test]
    public async Task FetchAsync_MalformedResponse_Reconcile_DoesNotDeleteLocal()
    {
        SetupMalformedRemote();

        var result = await m_Service.FetchAsync(
            MakeInput(reconcile: true),
            new[] { new AuthoringFile("dir/lobby.lo") },
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Failed, Has.Count.EqualTo(1));
        Assert.That(result.Deleted, Is.Empty);
        m_MockFileSystem.Verify(
            fs => fs.File.Delete(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task FetchAsync_MalformedResponse_NoReconcile_Fails()
    {
        SetupMalformedRemote();

        var result = await m_Service.FetchAsync(
            MakeInput(),
            new[] { new AuthoringFile("dir/lobby.lo") },
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Failed, Has.Count.EqualTo(1));
        Assert.That(result.Fetched, Is.Empty);
    }

    static FetchInput MakeInput(bool dryRun = false, bool reconcile = false)
    {
        return new FetchInput
        {
            Path = "dir",
            CloudProjectId = "test-project",
            DryRun = dryRun,
            Reconcile = reconcile,
        };
    }

    void RebuildWithFileMock(Action<Mock<IFile>> configureFile)
    {
        var mockFile = new Mock<IFile>();
        configureFile(mockFile);
        m_MockFileSystem.Setup(fs => fs.File).Returns(mockFile.Object);

        var mockDir = new Mock<IDirectory>();
        m_MockFileSystem.Setup(fs => fs.Directory).Returns(mockDir.Object);

        m_Handler = new LobbyFetchHandler(
            m_MockRemoteConfigService.Object,
            m_MockFileSystem.Object);
        m_Service = new LobbyFetchService(m_Handler);
    }

    void SetupRemoteConfig() => SetupRemoteConfigWithSchemaId("lobby");

    void SetupRemoteConfigWithSchemaId(string schemaId)
    {
        m_MockRemoteConfigService
            .Setup(r => r.GetAllConfigsFromEnvironmentAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonConvert.SerializeObject(new RemoteConfigResponse
            {
                Configs = new List<RemoteConfigValue>
                {
                    new()
                    {
                        Id = "mock_id",
                        Type = LobbyConstants.ConfigType,
                        Value = new List<LobbyConfigValue>
                        {
                            new()
                            {
                                Key = LobbyConstants.ConfigKey,
                                SchemaId = schemaId,
                                Value = new JObject
                                {
                                    ["activeLifespanSeconds"] = 300
                                },
                            }
                        },
                    }
                }
            }));
    }

    void SetupNoRemote()
    {
        m_MockRemoteConfigService
            .Setup(r => r.GetAllConfigsFromEnvironmentAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonConvert.SerializeObject(new RemoteConfigResponse()));
    }

    void SetupMalformedRemote()
    {
        m_MockRemoteConfigService
            .Setup(r => r.GetAllConfigsFromEnvironmentAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("not valid json {{{");
    }
}
