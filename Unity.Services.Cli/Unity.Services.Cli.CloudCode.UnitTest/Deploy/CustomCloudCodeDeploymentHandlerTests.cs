using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.CloudCode.Authoring.Editor.Core.Analytics;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Logging;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.DeploymentApi.Editor;
using Assert = NUnit.Framework.Assert;

namespace Unity.Services.Cli.CloudCode.UnitTest.Deploy;

[TestFixture]
class CustomCloudCodeDeploymentHandlerTests
{
    static readonly Mock<ICloudCodeClient> k_MockICloudCodeClient = new();
    static readonly Mock<IDeploymentAnalytics> k_DeploymentAnalytics = new();
    static readonly Mock<ILogger> k_Logger = new();
    static readonly Mock<IPreDeployValidator> k_PreDeployValidator = new();

    readonly CloudCodeScript m_ExpectedContent = new("name2", "test-path-2", 0, new DeploymentStatus(Statuses.Loading));

    readonly ExposeCliCloudCodeDeploymentHandler m_CliCloudCodeDeploymentHandler = new(
        k_MockICloudCodeClient.Object,
        k_DeploymentAnalytics.Object,
        k_Logger.Object,
        k_PreDeployValidator.Object);

    [Test]
    public void UpdateScriptProgressContentProgressUpdated()
    {
        const float expectedProgress = 100;
        m_CliCloudCodeDeploymentHandler.ExposeUpdateScriptProgress(m_ExpectedContent, expectedProgress);
        Assert.AreEqual(expectedProgress, m_ExpectedContent.Progress);
    }

    [Test]
    public void UpdateScriptStatusContentStatusUpdated()
    {
        const string expectedStatus = "Failed to Read";
        const string expectedDetail = "Reason for failed to read";
        m_CliCloudCodeDeploymentHandler.ExposeUpdateScriptStatus(m_ExpectedContent, expectedStatus, expectedDetail);
        Assert.AreEqual(expectedStatus, m_ExpectedContent.Status.Message);
        Assert.AreEqual(expectedDetail, m_ExpectedContent.Detail);
    }
}
