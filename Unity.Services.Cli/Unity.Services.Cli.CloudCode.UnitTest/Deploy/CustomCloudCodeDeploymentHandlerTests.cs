using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.CloudCode.Authoring.Editor.Core.Analytics;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Logging;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Assert = NUnit.Framework.Assert;

namespace Unity.Services.Cli.CloudCode.UnitTest.Deploy;

[TestFixture]
class CustomCloudCodeDeploymentHandlerTests
{
    static readonly Mock<ICloudCodeClient> k_MockICloudCodeClient = new();
    static readonly Mock<IDeploymentAnalytics> k_DeploymentAnalytics = new();
    static readonly Mock<IScriptCache> k_ScriptCache = new();
    static readonly Mock<ILogger> k_Logger = new();
    static readonly Mock<IPreDeployValidator> k_PreDeployValidator = new();
    static readonly Mock<IScript> k_MockScript = new();

    readonly DeployContent m_ExpectedContent = new("name2", "Cloud Code", "test-path-2", 0, "Reading");

    readonly ExposeCliCloudCodeDeploymentHandler m_CliCloudCodeDeploymentHandler = new(
        k_MockICloudCodeClient.Object,
        k_DeploymentAnalytics.Object,
        k_ScriptCache.Object,
        k_Logger.Object,
        k_PreDeployValidator.Object);

    [SetUp]
    public void SetUp()
    {
        m_CliCloudCodeDeploymentHandler.Contents.Clear();
        k_MockScript.Reset();

        m_CliCloudCodeDeploymentHandler.Contents.Add(new DeployContent("name1", "Cloud Code", "test-path-1", 0, "Reading"));
        m_CliCloudCodeDeploymentHandler.Contents.Add(m_ExpectedContent);

        k_MockScript.SetupGet(s => s.Path).Returns(m_ExpectedContent.Path);
    }

    [Test]
    public void UpdateScriptProgressContentProgressUpdated()
    {
        const float expectedProgress = 100;
        m_CliCloudCodeDeploymentHandler.ExposeUpdateScriptProgress(k_MockScript.Object, expectedProgress);
        Assert.AreEqual(expectedProgress, m_ExpectedContent.Progress);
    }

    [Test]
    public void UpdateScriptStatusContentStatusUpdated()
    {
        const string expectedStatus = "Failed to Read";
        const string expectedDetail = "Reason for failed to read";
        m_CliCloudCodeDeploymentHandler.ExposeUpdateScriptStatus(k_MockScript.Object, expectedStatus, expectedDetail);
        Assert.AreEqual(expectedStatus, m_ExpectedContent.Status);
        Assert.AreEqual(expectedDetail, m_ExpectedContent.Detail);
    }
}
