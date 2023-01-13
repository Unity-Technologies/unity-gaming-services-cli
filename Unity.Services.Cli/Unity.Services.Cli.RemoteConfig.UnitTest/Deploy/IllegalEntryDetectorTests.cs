using Moq;
using NUnit.Framework;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.ErrorHandling;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.RemoteConfig.UnitTest.Deploy;

[TestFixture]
public class IllegalEntryDetectorTests
{
    Mock<IRemoteConfigFile> m_MockIRemoteConfigFile = new();
    IllegalEntryDetector m_IllegalEntryDetector = new();

    [Test]
    public void ContainsIllegalEntries_ReturnsFalse()
    {
        Assert.That(m_IllegalEntryDetector.ContainsIllegalEntries(
            m_MockIRemoteConfigFile.Object,
            new List<RemoteConfigDeploymentException>()), Is.False);
    }
}
