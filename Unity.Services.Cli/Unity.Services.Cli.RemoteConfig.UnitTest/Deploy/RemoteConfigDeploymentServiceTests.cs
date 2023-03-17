using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.Cli.RemoteConfig.Exceptions;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Deployment;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.ErrorHandling;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;

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
    readonly Mock<ICliDeploymentOutputHandler> m_MockCliDeploymentOutputHandler = new();
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
        m_MockCliRemoteConfigClient.Reset();
        m_MockCliDeploymentOutputHandler.Reset();
        m_MockRemoteConfigScriptsLoader.Reset();
        m_MockRemoteConfigDeploymentHandler.Reset();
        k_MockRemoteConfigServicesWrapper.Reset();
        m_MockLogger.Reset();

        k_MockRemoteConfigServicesWrapper.Setup(x => x.RemoteConfigClient)
            .Returns(m_MockCliRemoteConfigClient.Object);
        k_MockRemoteConfigServicesWrapper.Setup(x => x.DeploymentOutputHandler)
            .Returns(m_MockCliDeploymentOutputHandler.Object);
        m_MockCliDeploymentOutputHandler.Setup(x => x.Contents)
            .Returns(new List<DeployContent>());
        k_MockRemoteConfigServicesWrapper.Setup(x => x.RemoteConfigScriptsLoader)
            .Returns(m_MockRemoteConfigScriptsLoader.Object);
        k_MockRemoteConfigServicesWrapper.Setup(x => x.DeploymentHandler)
            .Returns(m_MockRemoteConfigDeploymentHandler.Object);

        m_MockCliDeploymentOutputHandler.SetupGet(c => c.Contents).Returns(m_Contents);

        m_MockRemoteConfigScriptsLoader.Setup(loader =>
                loader.LoadScriptsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<ICollection<DeployContent>>()))
            .Returns(Task.FromResult(new LoadResult(new List<IRemoteConfigFile>(), new List<DeployContent>())));
        m_RemoteConfigDeploymentService = new (k_MockRemoteConfigServicesWrapper.Object);

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
                    k_MockRemoteConfigServicesWrapper.Object.DeploymentOutputHandler.Contents),
            Times.Once);
    }

    [Test]
    public async Task DeployAsync_CallsDeployAsyncCorrectly()
    {
        var expectedRemoteConfigFiles = new List<RemoteConfigFile>();

        m_MockRemoteConfigScriptsLoader.Setup(d =>
                d.LoadScriptsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<ICollection<DeployContent>>()))
            .ReturnsAsync(new LoadResult(new List<IRemoteConfigFile>(), new List<DeployContent>()));

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
}
