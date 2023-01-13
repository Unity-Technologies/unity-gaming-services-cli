using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Deploy.Service;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Deployment;

namespace Unity.Services.Cli.RemoteConfig.UnitTest.Service;

[TestFixture]
public class RemoteConfigServicesWrapperTests
{
    RemoteConfigServicesWrapper? m_RemoteConfigServicesWrapper;

    Mock<IRemoteConfigDeploymentHandler> m_DeploymentHandler = new();
    Mock<ICliRemoteConfigClient> m_RemoteConfigClient = new();
    Mock<ICliDeploymentOutputHandler> m_DeploymentOutputHandler = new();
    Mock<IDeployFileService> m_DeployFileService = new();
    Mock<IRemoteConfigService> m_RemoteConfigService = new();
    Mock<IRemoteConfigScriptsLoader> m_RemoteConfigScriptsLoader = new();

    [Test]
    public void GetAllConfigsFromEnvironmentAsync_InvalidProjectIDThrowConfigValidationException()
    {
        m_RemoteConfigServicesWrapper = new RemoteConfigServicesWrapper(
            m_DeploymentHandler.Object,
            m_RemoteConfigClient.Object,
            m_DeploymentOutputHandler.Object,
            m_DeployFileService.Object,
            m_RemoteConfigService.Object,
            m_RemoteConfigScriptsLoader.Object);

        Assert.Multiple(() =>
        {
            Assert.That(m_RemoteConfigServicesWrapper.DeploymentHandler, Is.EqualTo(m_DeploymentHandler.Object));
            Assert.That(m_RemoteConfigServicesWrapper.RemoteConfigClient, Is.EqualTo(m_RemoteConfigClient.Object));
            Assert.That(m_RemoteConfigServicesWrapper.DeploymentOutputHandler, Is.EqualTo(m_DeploymentOutputHandler.Object));
            Assert.That(m_RemoteConfigServicesWrapper.DeployFileService, Is.EqualTo(m_DeployFileService.Object));
            Assert.That(m_RemoteConfigServicesWrapper.RemoteConfigService, Is.EqualTo(m_RemoteConfigService.Object));
            Assert.That(m_RemoteConfigServicesWrapper.RemoteConfigScriptsLoader, Is.EqualTo(m_RemoteConfigScriptsLoader.Object));
        });
    }
}
