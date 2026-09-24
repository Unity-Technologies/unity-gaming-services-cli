using System.IO.Abstractions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Lobby.Deploy;
using Unity.Services.Cli.Lobby.Handlers;
using Unity.Services.Cli.Lobby.Handlers.Config;
using Unity.Services.Cli.RemoteConfig.Exceptions;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.Cli.RemoteConfig.Types;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Lobby.UnitTest.Deploy;

[TestFixture]
class LobbyDeploymentServiceTests
{
    readonly Mock<IRemoteConfigService> m_MockRemoteConfigService = new();
    readonly Mock<IFileSystem> m_MockFileSystem = new();

    LobbyDeploymentHandler m_Handler = null!;
    LobbyResourceLoader m_ResourceLoader = null!;
    LobbyDeploymentService m_Service = null!;

    const string k_FilePath = "dir/lobby.lo";

    const string k_ValidJson = """
        {
          "schemaId": "lobby",
          "activeLifespanSeconds": 300,
          "disconnectRemovalTimeSeconds": 30,
          "playerSlots": { "minimum": 1, "maximum": 10 },
          "socialProfilesEnabled": false
        }
        """;

    [SetUp]
    public void SetUp()
    {
        m_MockRemoteConfigService.Reset();
        m_MockFileSystem.Reset();

        m_Handler = new LobbyDeploymentHandler(m_MockRemoteConfigService.Object);
        m_ResourceLoader = new LobbyResourceLoader(m_MockFileSystem.Object);
        m_Service = new LobbyDeploymentService(m_Handler, m_ResourceLoader);
    }

    [Test]
    public async Task Deploy_WithNoFiles_ReturnsEmptyResult()
    {
        SetupNoRemote();

        var result = await m_Service.Deploy(
            MakeInput(),
            Array.Empty<AuthoringFile>(),
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Deployed, Is.Empty);
        Assert.That(result.Failed, Is.Empty);
    }

    [Test]
    public async Task Deploy_WhenRemoteExists_UpdatesConfig()
    {
        SetupFileRead(k_ValidJson);
        SetupExistingRemote();

        var result = await m_Service.Deploy(
            MakeInput(),
            new[] { new AuthoringFile(k_FilePath) },
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Updated, Has.Count.EqualTo(1));
        Assert.That(result.Created, Is.Empty);
        m_MockRemoteConfigService.Verify(
            r => r.UpdateConfigAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        m_MockRemoteConfigService.Verify(
            r => r.CreateConfigAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<ConfigValue>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Deploy_WhenRemoteDoesNotExist_CreatesConfig()
    {
        SetupFileRead(k_ValidJson);
        SetupNoRemote();

        var result = await m_Service.Deploy(
            MakeInput(),
            new[] { new AuthoringFile(k_FilePath) },
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Created, Has.Count.EqualTo(1));
        Assert.That(result.Updated, Is.Empty);
        m_MockRemoteConfigService.Verify(
            r => r.CreateConfigAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<ConfigValue>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Deploy_DryRun_DoesNotCallApi()
    {
        SetupFileRead(k_ValidJson);
        SetupExistingRemote();

        var result = await m_Service.Deploy(
            MakeInput(dryRun: true),
            new[] { new AuthoringFile(k_FilePath) },
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Deployed, Has.Count.EqualTo(1));
        m_MockRemoteConfigService.Verify(
            r => r.UpdateConfigAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestCase("lobby", ".*resource:/lobby.*")]
    [TestCase("lobbyv2", ".*resource:/lobbyv2.*")]
    [TestCase("lobbyv3", ".*resource:/lobbyv3.*")]
    public async Task Deploy_AppliesCorrectSchema(string schemaId, string expectedPattern)
    {
        var json = $$"""
            { "schemaId": "{{schemaId}}", "activeLifespanSeconds": 300, "disconnectRemovalTimeSeconds": 30, "playerSlots": { "minimum": 1, "maximum": 10 } }
            """;
        SetupFileRead(json);
        SetupExistingRemote();

        await m_Service.Deploy(
            MakeInput(),
            new[] { new AuthoringFile(k_FilePath) },
            "proj", "env", null, CancellationToken.None);

        m_MockRemoteConfigService.Verify(
            r => r.ApplySchemaAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsRegex(expectedPattern), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Deploy_WhenApplySchemaThrowsApiException_ItemIsInFailed()
    {
        SetupFileRead(k_ValidJson);
        SetupExistingRemote();
        m_MockRemoteConfigService
            .Setup(r => r.ApplySchemaAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException("schema error", 1));

        var result = await m_Service.Deploy(
            MakeInput(),
            new[] { new AuthoringFile(k_FilePath) },
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Failed, Has.Count.EqualTo(1));
        Assert.That(result.Failed[0].Status.MessageSeverity, Is.EqualTo(SeverityLevel.Error));
        Assert.That(result.Deployed, Is.Empty);
    }

    [Test]
    public async Task Deploy_WhenUpdateThrows_ItemIsInFailed()
    {
        SetupFileRead(k_ValidJson);
        SetupExistingRemote();
        m_MockRemoteConfigService
            .Setup(r => r.ApplySchemaAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        m_MockRemoteConfigService
            .Setup(r => r.UpdateConfigAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("api error"));

        var result = await m_Service.Deploy(
            MakeInput(),
            new[] { new AuthoringFile(k_FilePath) },
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Failed, Has.Count.EqualTo(1));
        Assert.That(result.Deployed, Is.Empty);
    }

    [Test]
    public async Task Deploy_InvalidJson_ItemInFailed()
    {
        SetupFileRead("not valid json {{{");

        var result = await m_Service.Deploy(
            MakeInput(),
            new[] { new AuthoringFile(k_FilePath) },
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Failed, Has.Count.EqualTo(1));
        Assert.That(result.Failed[0].Status.MessageSeverity, Is.EqualTo(SeverityLevel.Error));
    }

    [Test]
    public async Task Deploy_EmptyJsonObject_ItemInFailed()
    {
        SetupFileRead("{}");

        var result = await m_Service.Deploy(
            MakeInput(),
            new[] { new AuthoringFile(k_FilePath) },
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Failed, Has.Count.EqualTo(1));
        Assert.That(result.Failed[0].Status.MessageSeverity, Is.EqualTo(SeverityLevel.Error));
    }

    [Test]
    public async Task Deploy_FlatConfigWithoutSchemaId_DefaultsToV1()
    {
        var json = """{ "activeLifespanSeconds": 300, "disconnectRemovalTimeSeconds": 30, "playerSlots": { "minimum": 1, "maximum": 10 } }""";
        SetupFileRead(json);
        SetupExistingRemote();

        await m_Service.Deploy(
            MakeInput(),
            new[] { new AuthoringFile(k_FilePath) },
            "proj", "env", null, CancellationToken.None);

        m_MockRemoteConfigService.Verify(
            r => r.ApplySchemaAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsRegex(".*resource:/lobby.*"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Deploy_MultipleLoFiles_OnlyFirstDeployed_ExtrasFailed()
    {
        SetupFileRead(k_ValidJson);
        SetupExistingRemote();

        var files = new[]
        {
            new AuthoringFile("dir/lobby.lo"),
            new AuthoringFile("dir/other.lo"),
            new AuthoringFile("dir/third.lo"),
        };

        var result = await m_Service.Deploy(
            MakeInput(),
            files,
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Deployed, Has.Count.EqualTo(1));
        Assert.That(result.Deployed[0].Path, Is.EqualTo("dir/lobby.lo"));
        Assert.That(result.Failed, Has.Count.EqualTo(2));
        Assert.That(result.Failed.All(f => f.Status.MessageSeverity == SeverityLevel.Error), Is.True);
        Assert.That(result.Failed.All(f => f.Status.MessageDetail!.Contains("Only one")), Is.True);
    }

    [Test]
    public async Task Deploy_MultipleLoFiles_FirstInvalid_AllFailed()
    {
        SetupFileRead("not valid json {{{");

        var files = new[]
        {
            new AuthoringFile("dir/bad.lo"),
            new AuthoringFile("dir/other.lo"),
        };

        var result = await m_Service.Deploy(
            MakeInput(),
            files,
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Deployed, Is.Empty);
        Assert.That(result.Failed, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Deploy_Reconcile_NoFiles_RemoteExists_DeletesRemoteConfig()
    {
        SetupExistingRemote();
        m_MockRemoteConfigService
            .Setup(r => r.DeleteConfigAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await m_Service.Deploy(
            MakeInput(reconcile: true),
            Array.Empty<AuthoringFile>(),
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Deleted, Has.Count.EqualTo(1));
        Assert.That(result.Deleted[0].Status.MessageSeverity, Is.EqualTo(SeverityLevel.Success));
        Assert.That(result.Failed, Is.Empty);
        m_MockRemoteConfigService.Verify(
            r => r.DeleteConfigAsync(
                "proj", "mock_id", LobbyConstants.ConfigType, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Deploy_Reconcile_NoFiles_NoRemote_ReturnsEmpty()
    {
        SetupNoRemote();

        var result = await m_Service.Deploy(
            MakeInput(reconcile: true),
            Array.Empty<AuthoringFile>(),
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Deployed, Is.Empty);
        Assert.That(result.Deleted, Is.Empty);
        Assert.That(result.Failed, Is.Empty);
    }

    [Test]
    public async Task Deploy_Reconcile_NoFiles_DryRun_DoesNotCallDelete()
    {
        SetupExistingRemote();

        var result = await m_Service.Deploy(
            MakeInput(dryRun: true, reconcile: true),
            Array.Empty<AuthoringFile>(),
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Deleted, Has.Count.EqualTo(1));
        m_MockRemoteConfigService.Verify(
            r => r.DeleteConfigAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Deploy_Reconcile_NoFiles_DeleteThrows_ItemInFailed()
    {
        SetupExistingRemote();
        m_MockRemoteConfigService
            .Setup(r => r.DeleteConfigAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("delete failed"));

        var result = await m_Service.Deploy(
            MakeInput(reconcile: true),
            Array.Empty<AuthoringFile>(),
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Failed, Has.Count.EqualTo(1));
        Assert.That(result.Failed[0].Status.MessageDetail, Does.Contain("delete failed"));
        Assert.That(result.Deleted, Is.Empty);
    }

    [Test]
    public async Task Deploy_MalformedRemoteResponse_Fails()
    {
        SetupFileRead(k_ValidJson);
        SetupMalformedRemote();

        var result = await m_Service.Deploy(
            MakeInput(),
            new[] { new AuthoringFile(k_FilePath) },
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Failed, Has.Count.EqualTo(1));
        Assert.That(result.Deployed, Is.Empty);
    }

    [Test]
    public async Task Deploy_Reconcile_MalformedRemoteResponse_Fails()
    {
        SetupMalformedRemote();

        var result = await m_Service.Deploy(
            MakeInput(reconcile: true),
            Array.Empty<AuthoringFile>(),
            "proj", "env", null, CancellationToken.None);

        Assert.That(result.Failed, Has.Count.EqualTo(1));
        Assert.That(result.Deleted, Is.Empty);
    }

    static DeployInput MakeInput(bool dryRun = false, bool reconcile = false)
    {
        return new DeployInput
        {
            Paths = Array.Empty<string>(),
            CloudProjectId = "test-project",
            DryRun = dryRun,
            Reconcile = reconcile,
        };
    }

    void SetupFileRead(string content)
    {
        var mockFile = new Mock<IFile>();
        mockFile.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);
        m_MockFileSystem.Setup(fs => fs.File).Returns(mockFile.Object);
    }

    void SetupExistingRemote()
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
                                SchemaId = "lobby",
                                Value = new Newtonsoft.Json.Linq.JObject { ["activeLifespanSeconds"] = 300 },
                            }
                        },
                    }
                }
            }));
        m_MockRemoteConfigService
            .Setup(r => r.ApplySchemaAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        m_MockRemoteConfigService
            .Setup(r => r.UpdateConfigAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
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
