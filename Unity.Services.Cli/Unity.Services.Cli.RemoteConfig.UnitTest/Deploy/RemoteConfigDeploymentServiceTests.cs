using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Deploy.Input;
using Unity.Services.Cli.Deploy.Model;
using Unity.Services.Cli.Deploy.Service;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Deployment;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.ErrorHandling;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.RemoteConfig.UnitTest.Deploy;

[TestFixture]
public class RemoteConfigDeploymentServiceTests
{
    RemoteConfigDeploymentService? m_RemoteConfigDeploymentService;
    const string k_ValidProjectId = "a912b1fd-541d-42e1-89f2-85436f27aabd";
    const string k_ValidEnvironmentId = "00000000-0000-0000-0000-000000000000";
    const string k_DeployFileExtension = ".rc";

    static readonly List<string> k_ValidFilePaths = new()
    {
        "test_a.rc",
        "test_b.rc"
    };

    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ICliRemoteConfigClient> m_MockCliRemoteConfigClient = new();
    readonly Mock<ICliDeploymentOutputHandler> m_MockCliDeploymentOutputHandler = new();
    readonly Mock<IDeployFileService> m_MockDeployFileService = new();
    readonly Mock<IRemoteConfigScriptsLoader> m_MockRemoteConfigScriptsLoader = new();
    readonly Mock<IRemoteConfigDeploymentHandler> m_MockRemoteConfigDeploymentHandler = new();

    readonly Mock<IRemoteConfigServicesWrapper> m_MockRemoteConfigServicesWrapper = new();
    readonly Mock<ILogger> m_MockLogger = new();

    DeployInput m_DefaultInput = new()
    {
        CloudProjectId = k_ValidProjectId
    };

    static readonly IReadOnlyCollection<DeployContent> k_DeployedContents = new[]
    {
        new DeployContent("script.rc", "Remote Config", "path", 100, "Published"),
    };

    static readonly IReadOnlyCollection<DeployContent> k_FailedContents = new[]
    {
        new DeployContent("invalid1.rc", "Remote Config", "path", 0, "Failed to Load"),
        new DeployContent("invalid2.rc", "Remote Config", "path", 0, "Failed to Load"),
    };

    List<DeployContent> m_Contents = k_DeployedContents.Concat(k_FailedContents).ToList();

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockCliRemoteConfigClient.Reset();
        m_MockCliDeploymentOutputHandler.Reset();
        m_MockDeployFileService.Reset();
        m_MockRemoteConfigScriptsLoader.Reset();
        m_MockRemoteConfigDeploymentHandler.Reset();
        m_MockRemoteConfigServicesWrapper.Reset();
        m_MockLogger.Reset();

        m_MockRemoteConfigServicesWrapper.Setup(x => x.RemoteConfigClient)
            .Returns(m_MockCliRemoteConfigClient.Object);
        m_MockRemoteConfigServicesWrapper.Setup(x => x.DeploymentOutputHandler)
            .Returns(m_MockCliDeploymentOutputHandler.Object);
        m_MockCliDeploymentOutputHandler.Setup(x => x.Contents)
            .Returns(new List<DeployContent>());
        m_MockRemoteConfigServicesWrapper.Setup(x => x.DeployFileService)
            .Returns(m_MockDeployFileService.Object);
        m_MockRemoteConfigServicesWrapper.Setup(x => x.RemoteConfigScriptsLoader)
            .Returns(m_MockRemoteConfigScriptsLoader.Object);
        m_MockRemoteConfigServicesWrapper.Setup(x => x.DeploymentHandler)
            .Returns(m_MockRemoteConfigDeploymentHandler.Object);

        m_MockCliDeploymentOutputHandler.SetupGet(c => c.Contents).Returns(m_Contents);

        m_RemoteConfigDeploymentService =
            new RemoteConfigDeploymentService(m_MockUnityEnvironment.Object, m_MockRemoteConfigServicesWrapper.Object);

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(k_ValidEnvironmentId);
        m_MockDeployFileService.Setup(d => d.ListFilesToDeploy(m_DefaultInput.Paths, k_DeployFileExtension))
            .Returns(k_ValidFilePaths);
    }

    [Test]
    public void DeployAsync_DoesNotThrowOnRemoteConfigDeploymentException()
    {
        m_MockRemoteConfigDeploymentHandler.Setup(ex => ex
            //RemoteConfigDeploymentException is sealed, so we're throwing RequestFailedException which inherits from the sealed one
            .DeployAsync(It.IsAny<IReadOnlyList<IRemoteConfigFile>>(), false)).ThrowsAsync(new RequestFailedException(1, ""));

        Assert.DoesNotThrowAsync(() => m_RemoteConfigDeploymentService.Deploy(
            m_DefaultInput,
            (StatusContext)null!,
            CancellationToken.None));
    }

    [Test]
    public async Task DeployAsync_CallsFetchIdentifierAsync()
    {
        await m_RemoteConfigDeploymentService.Deploy(
            m_DefaultInput,
            (StatusContext)null!,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(), Times.Once);
    }

    [Test]
    public async Task DeployAsync_CallsInitializeCorrectly()
    {
        await m_RemoteConfigDeploymentService.Deploy(
            m_DefaultInput,
            (StatusContext)null!,
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
            (StatusContext)null!,
            CancellationToken.None);

        m_MockRemoteConfigScriptsLoader.Verify(x =>
                x.LoadScriptsAsync(
                    k_ValidFilePaths,
                    m_MockRemoteConfigServicesWrapper.Object.DeploymentOutputHandler.Contents),
            Times.Once);
    }

    [Test]
    public async Task DeployAsync_CallsDeployAsyncCorrectly()
    {
        var expectedRemoteConfigFiles = new List<RemoteConfigFile>();

        m_MockRemoteConfigScriptsLoader.Setup(d =>
                d.LoadScriptsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<ICollection<DeployContent>>()))
            .ReturnsAsync(expectedRemoteConfigFiles);

        await m_RemoteConfigDeploymentService.Deploy(
            m_DefaultInput,
            (StatusContext)null!,
            CancellationToken.None);

        m_MockRemoteConfigDeploymentHandler.Verify(x => x.DeployAsync(expectedRemoteConfigFiles, false), Times.Once);
    }

    [Test]
    public async Task DeployAsync_CallsLogDeploymentResultCorrectly()
    {
        var result = await m_RemoteConfigDeploymentService.Deploy(
            m_DefaultInput,
            (StatusContext)null!,
            CancellationToken.None);

        Assert.That(result.Deployed, Is.EqualTo(k_DeployedContents));
        Assert.That(result.Failed, Is.EqualTo(k_FailedContents));

    }
}
