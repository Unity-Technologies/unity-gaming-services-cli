using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.Cli.RemoteConfig.Exceptions;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Deployment;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.ErrorHandling;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Results;
using RemoteConfigFile = Unity.Services.Cli.RemoteConfig.Deploy.RemoteConfigFile;

namespace Unity.Services.Cli.RemoteConfig.UnitTest.Deploy;

[TestFixture]
public class RemoteConfigDeploymentServiceTests
{

    const string k_ValidProjectId = "a912b1fd-541d-42e1-89f2-85436f27aabd";
    const string k_ValidEnvironmentId = "00000000-0000-0000-0000-000000000000";

    static readonly List<string> k_ValidFilePaths = new()
    {
        "test_a.rc",
        "test_b.rc"
    };

    readonly Mock<ICliRemoteConfigClient> m_MockCliRemoteConfigClient = new();
    readonly Mock<IRemoteConfigScriptsLoader> m_MockRemoteConfigScriptsLoader = new();
    readonly Mock<IRemoteConfigDeploymentHandler> m_MockRemoteConfigDeploymentHandler = new();

    static readonly Mock<IRemoteConfigServicesWrapper> k_MockRemoteConfigServicesWrapper = new();
    readonly Mock<ILogger> m_MockLogger = new();

    RemoteConfigDeploymentService m_RemoteConfigDeploymentService = new (k_MockRemoteConfigServicesWrapper.Object);

    DeployInput m_DefaultInput = new()
    {
        CloudProjectId = k_ValidProjectId,
        Reconcile = false
    };

    static readonly IReadOnlyList<RemoteConfigFile> k_DeployedContents = new[]
    {
        new RemoteConfigFile("script.rc", "path"),
    };

    static readonly IReadOnlyList<RemoteConfigFile> k_FailedContents = new[]
    {
        new RemoteConfigFile("invalid1.rc", "path"),
        new RemoteConfigFile("invalid2.rc",  "path"),
    };



    [SetUp]
    public void SetUp()
    {
        m_MockCliRemoteConfigClient.Reset();
        m_MockRemoteConfigScriptsLoader.Reset();
        m_MockRemoteConfigDeploymentHandler.Reset();
        k_MockRemoteConfigServicesWrapper.Reset();
        m_MockLogger.Reset();

        k_MockRemoteConfigServicesWrapper.Setup(x => x.RemoteConfigClient)
            .Returns(m_MockCliRemoteConfigClient.Object);
        k_MockRemoteConfigServicesWrapper.Setup(x => x.RemoteConfigScriptsLoader)
            .Returns(m_MockRemoteConfigScriptsLoader.Object);
        k_MockRemoteConfigServicesWrapper.Setup(x => x.DeploymentHandler)
            .Returns(m_MockRemoteConfigDeploymentHandler.Object);

        m_MockRemoteConfigScriptsLoader.Setup(loader =>
                loader.LoadScriptsAsync(It.IsAny<IReadOnlyList<string>>(),  CancellationToken.None))
            .Returns(Task.FromResult(new LoadResult(new List<RemoteConfigFile>(), new List<RemoteConfigFile>())));
        m_RemoteConfigDeploymentService = new (k_MockRemoteConfigServicesWrapper.Object);

        m_MockRemoteConfigDeploymentHandler.Setup(ex => ex
                .DeployAsync(It.IsAny<IReadOnlyList<IRemoteConfigFile>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(Task.FromResult(new DeployResult(
                Array.Empty<RemoteConfigEntry>(),
                Array.Empty<RemoteConfigEntry>(),
                Array.Empty<RemoteConfigEntry>()
            )));

        k_DeployedContents[0].Progress = 100;
        k_FailedContents[0].Status = new DeploymentStatus(Statuses.FailedToRead, "invalid1", SeverityLevel.Error);
        k_FailedContents[1].Status = new DeploymentStatus(Statuses.FailedToRead, "invalid2", SeverityLevel.Error);
    }

    [Test]
    public void DeployAsync_DoesNotThrowOnRemoteConfigDeploymentException()
    {
        m_MockRemoteConfigDeploymentHandler.Setup(ex => ex
            //RemoteConfigDeploymentException is sealed, so we're throwing RequestFailedException which inherits from the sealed one
            .DeployAsync(It.IsAny<IReadOnlyList<IRemoteConfigFile>>(), false, false))
            .ThrowsAsync(new RequestFailedException(1, ""));

        Assert.DoesNotThrowAsync(() => m_RemoteConfigDeploymentService.Deploy(
            m_DefaultInput,
            k_ValidFilePaths,
            k_ValidProjectId,
            k_ValidEnvironmentId,
            null!,
            CancellationToken.None));
    }

    [Test]
    public void DeployAsync_DoesNotThrowOnApiException()
    {
        m_MockRemoteConfigDeploymentHandler.Setup(ex => ex
                .DeployAsync(It.IsAny<IReadOnlyList<IRemoteConfigFile>>(), false, false))
            .ThrowsAsync(new ApiException("", 1));

        Assert.DoesNotThrowAsync(() => m_RemoteConfigDeploymentService.Deploy(
            m_DefaultInput,
            k_ValidFilePaths,
            k_ValidProjectId,
            k_ValidEnvironmentId,
            null!,
            CancellationToken.None));
    }

    [Test]
    public async Task DeployAsync_CallsInitializeCorrectly()
    {
        await m_RemoteConfigDeploymentService.Deploy(
            m_DefaultInput,
            k_ValidFilePaths,
            k_ValidProjectId,
            k_ValidEnvironmentId,
            null!,
            CancellationToken.None);

        m_MockCliRemoteConfigClient.Verify(
            x => x.Initialize(
                k_ValidProjectId,
                k_ValidEnvironmentId,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task DeployAsync_CallsLoadScriptsAsyncCorrectly()
    {
        await m_RemoteConfigDeploymentService.Deploy(
            m_DefaultInput,
            k_ValidFilePaths,
            k_ValidProjectId,
            k_ValidEnvironmentId,
            null!,
            CancellationToken.None);

        m_MockRemoteConfigScriptsLoader.Verify(x =>
                x.LoadScriptsAsync(
                    k_ValidFilePaths,
                    CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task DeployAsync_CallsDeployAsyncCorrectly()
    {
        var expectedRemoteConfigFiles = new List<RemoteConfigFile>();

        m_MockRemoteConfigScriptsLoader.Setup(d =>
                d.LoadScriptsAsync(It.IsAny<IReadOnlyList<string>>(),  CancellationToken.None))
            .ReturnsAsync(new LoadResult(new List<RemoteConfigFile>(), new List<RemoteConfigFile>()));

        await m_RemoteConfigDeploymentService.Deploy(
            m_DefaultInput,
            k_ValidFilePaths,
            k_ValidProjectId,
            k_ValidEnvironmentId,
            null!,
            CancellationToken.None);

        m_MockRemoteConfigDeploymentHandler.Verify(x =>
            x.DeployAsync(expectedRemoteConfigFiles, false, false), Times.Once);
    }

    [Test]
    public async Task DeployAsync_CallsLogDeploymentResultCorrectly()
    {
        m_MockRemoteConfigDeploymentHandler.Reset();
        m_MockRemoteConfigScriptsLoader
            .Setup(l =>
                    l.LoadScriptsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new LoadResult(k_DeployedContents, k_FailedContents)));

        var result = await m_RemoteConfigDeploymentService.Deploy(
            m_DefaultInput,
            k_ValidFilePaths,
            k_ValidProjectId,
            k_ValidEnvironmentId,
            null!,
            CancellationToken.None);

        Assert.That(result.Deployed, Is.EqualTo(k_DeployedContents));
        Assert.That(result.Failed, Is.EqualTo(k_FailedContents));
    }

    [Test]
    public async Task DeployAsync_RemoteEntriesHaveCorrectFilePath()
    {
        m_MockRemoteConfigDeploymentHandler
            .Setup(ex =>
                ex.DeployAsync(
                    It.IsAny<IReadOnlyList<IRemoteConfigFile>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
            .Returns(Task.FromResult(new DeployResult(
                Array.Empty<RemoteConfigEntry>(),
                Array.Empty<RemoteConfigEntry>(),
                new []
                {
                    new RemoteConfigEntry()
                    {
                        File = null,
                        Key = "Remote"
                    }
                }
            )));

        var result = await m_RemoteConfigDeploymentService.Deploy(
            m_DefaultInput,
            k_ValidFilePaths,
            k_ValidProjectId,
            k_ValidEnvironmentId,
            null!,
            CancellationToken.None);

        Assert.That(result.Deleted.Any(deployContent=> deployContent.Path == "Remote"));
    }

    [Test]
    public async Task DeployAsync_EmptyRemoteFilesAreSkipped()
    {
        m_MockRemoteConfigDeploymentHandler
            .Setup(ex => ex
                .DeployAsync(It.IsAny<IReadOnlyList<IRemoteConfigFile>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(Task.FromResult(new DeployResult(
                Array.Empty<RemoteConfigEntry>(),
                Array.Empty<RemoteConfigEntry>(),
                Array.Empty<RemoteConfigEntry>(),
                Array.Empty<RemoteConfigFile>()
            )));

        var result = await m_RemoteConfigDeploymentService.Deploy(
            m_DefaultInput,
            k_ValidFilePaths,
            k_ValidProjectId,
            k_ValidEnvironmentId,
            null!,
            CancellationToken.None);

        Assert.That(result.Deployed.Count, Is.EqualTo(0));
    }
}
