using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Exceptions;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.UnitTest.Utils;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.CloudCode.Authoring;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;
using AuthoringLanguage = Unity.Services.CloudCode.Authoring.Editor.Core.Model.Language;

namespace Unity.Services.Cli.CloudCode.UnitTest.Deploy;

[TestFixture]
public class CloudCodeDeploymentServiceTests
{
    const string k_InvalidScriptFile = "inValidscript.js";

    static readonly List<string> k_ValidFilePaths = new()
    {
        "test_a.js",
        "test_b.js"
    };

    readonly Mock<IJavaScriptClient> m_MockCloudCodeClient = new();
    readonly Mock<ICliEnvironmentProvider> m_MockEnvironmentProvider = new();
    readonly Mock<ICloudCodeInputParser> m_MockCloudCodeInputParser = new();
    readonly Mock<IDeploymentHandlerWithOutput> m_DeploymentHandlerWithOutput = new();
    readonly Mock<ICloudCodeScriptsLoader> m_MockCloudCodeScriptsLoader = new();
    readonly Mock<ICloudCodeScriptParser> m_MockCloudCodeScriptParser = new();
    readonly Mock<ILogger> m_MockLogger = new();

    static readonly IReadOnlyCollection<DeployContent> k_DeployedContents = new[]
    {
        new DeployContent("script.js", "Cloud Code", "path", 100, "Published"),
    };

    static readonly IReadOnlyCollection<DeployContent> k_FailedContents = new[]
    {
        new DeployContent("invalid1.js", "Cloud Code", "path", 0, "Failed to Load"),
        new DeployContent("invalid2.js", "Cloud Code", "path", 0, "Failed to Load"),
    };

    readonly List<ScriptInfo> m_RemoteContents = new()
    {
        new ScriptInfo("ToDelete", ".js")
    };

    readonly List<DeployContent> m_Contents = k_DeployedContents.Concat(k_FailedContents).ToList();

    CloudCodeScriptDeploymentService? m_DeploymentService;

    [SetUp]
    public void SetUp()
    {
        m_MockCloudCodeClient.Reset();
        m_MockEnvironmentProvider.Reset();
        m_MockCloudCodeInputParser.Reset();
        m_DeploymentHandlerWithOutput.Reset();
        m_MockLogger.Reset();
        m_MockCloudCodeScriptsLoader.Reset();
        m_MockCloudCodeScriptParser.Reset();

        foreach (var scriptPath in k_ValidFilePaths)
        {
            m_MockCloudCodeInputParser.Setup(p => p.LoadScriptCodeAsync(scriptPath, CancellationToken.None))
                .ReturnsAsync(TestValues.ValidCode);
        }

        m_DeploymentHandlerWithOutput.SetupGet(c => c.Contents)
            .Returns(m_Contents);

        m_MockCloudCodeInputParser.Setup(p => p.LoadScriptCodeAsync(k_InvalidScriptFile, CancellationToken.None))
            .Throws(new ScriptEvaluationException(""));

        m_DeploymentService = new CloudCodeScriptDeploymentService(
            m_MockCloudCodeInputParser.Object,
            m_MockCloudCodeScriptParser.Object,
            m_DeploymentHandlerWithOutput.Object,
            m_MockCloudCodeScriptsLoader.Object,
            m_MockEnvironmentProvider.Object,
            m_MockCloudCodeClient.Object);

        m_MockCloudCodeScriptsLoader.Setup(
            c => c.LoadScriptsAsync(
                k_ValidFilePaths,
                "Cloud Code",
                ".js",
                m_MockCloudCodeInputParser.Object,
                m_MockCloudCodeScriptParser.Object,
                m_Contents,
                CancellationToken.None)).ReturnsAsync(new CloudCodeScriptLoadResult(new List<IScript>(), new List<DeployContent>()));
    }

    [Test]
    public async Task DeployAsync_CallsLoadFilePathsFromInputCorrectly()
    {
        CloudCodeInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
        };

        var result = await m_DeploymentService!.Deploy(
            input,
            k_ValidFilePaths,
            TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            null!,
            CancellationToken.None);

        m_MockCloudCodeClient.Verify(
            x => x.Initialize(
                TestValues.ValidEnvironmentId,
                TestValues.ValidProjectId,
                CancellationToken.None),
            Times.Once);
        m_MockEnvironmentProvider.VerifySet(x => { x.Current = TestValues.ValidEnvironmentId; }, Times.Once);
        m_DeploymentHandlerWithOutput.Verify(x => x.DeployAsync(It.IsAny<List<IScript>>(), false, false), Times.Once);
        Assert.AreEqual(k_DeployedContents, result.Deployed);
        Assert.AreEqual(k_FailedContents, result.Failed);
    }

    [Test]
    public async Task DeployReconcileAsync_WillCreateDeleteContent()
    {
        CloudCodeInput input = new()
        {
            Reconcile = true,
            CloudProjectId = TestValues.ValidProjectId,
        };

        m_DeploymentHandlerWithOutput.Setup(
                ex => ex.DeployAsync(It.IsAny<IEnumerable<IScript>>(), true, false))
            .Returns(
                Task.FromResult(
                    new DeployResult(
                        System.Array.Empty<IScript>(),
                        System.Array.Empty<IScript>(),
                        m_RemoteContents.Select(script => (IScript)script).ToList(),
                        System.Array.Empty<IScript>(),
                        System.Array.Empty<IScript>())));

        var result = await m_DeploymentService!.Deploy(
            input,
            k_ValidFilePaths,
            TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            null!,
            CancellationToken.None);

        Assert.IsTrue(
            result.Deleted.Any(
                item => m_RemoteContents.Any(content => content.Name.ToString() == item.Name)));
    }

    [Test]
    public void DeployAsync_DoesNotThrowOnApiException()
    {
        CloudCodeInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId
        };

        m_DeploymentHandlerWithOutput.Setup(
                ex => ex.DeployAsync(It.IsAny<IEnumerable<IScript>>(), false, false))
            .ThrowsAsync(new ApiException());

        Assert.DoesNotThrowAsync(
            () => m_DeploymentService!.Deploy(
                input,
                k_ValidFilePaths,
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                null!,
                CancellationToken.None));
    }
}
