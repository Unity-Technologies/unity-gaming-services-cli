using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Exceptions;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.UnitTest.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Deploy.Model;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Deploy.Service;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
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

    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ICliCloudCodeClient> m_MockCloudCodeClient = new();
    readonly Mock<ICliEnvironmentProvider> m_MockEnvironmentProvider = new();
    readonly Mock<ICloudCodeInputParser> m_MockCloudCodeInputParser = new();
    readonly Mock<ICloudCodeService> m_MockCloudCodeService = new();
    readonly Mock<ICloudCodeDeploymentHandler> m_MockCloudCodeDeploymentHandler = new();
    readonly Mock<ICloudCodeServicesWrapper> m_MockServicesWrapper = new();
    readonly Mock<IDeployFileService> m_MockDeployFileService = new();
    readonly Mock<ICliDeploymentOutputHandler> m_MockCliDeploymentOutputHandler = new();
    readonly Mock<ICloudCodeScriptsLoader> m_MockCloudCodeScriptsLoader = new();
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

    List<DeployContent> m_Contents = k_DeployedContents.Concat(k_FailedContents).ToList();

    CloudCodeDeploymentService? m_DeploymentService;

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockCloudCodeClient.Reset();
        m_MockEnvironmentProvider.Reset();
        m_MockCloudCodeInputParser.Reset();
        m_MockCloudCodeService.Reset();
        m_MockCloudCodeDeploymentHandler.Reset();
        m_MockServicesWrapper.Reset();
        m_MockDeployFileService.Reset();
        m_MockLogger.Reset();
        m_MockCliDeploymentOutputHandler.Reset();
        m_MockCloudCodeScriptsLoader.Reset();

        m_MockCloudCodeService.Setup(c => c.GetScriptParameters(TestValues.ValidCode, CancellationToken.None))
            .ReturnsAsync(
                new List<ScriptParameter>
                {
                    new("sides", ScriptParameter.TypeEnum.NUMERIC)
                });
        foreach (var scriptPath in k_ValidFilePaths)
        {
            m_MockCloudCodeInputParser.Setup(p => p.LoadScriptCodeAsync(scriptPath, CancellationToken.None))
                .ReturnsAsync(TestValues.ValidCode);
        }

        m_MockCliDeploymentOutputHandler.SetupGet(c => c.Contents).Returns(m_Contents);

        m_MockCloudCodeInputParser.Setup(p => p.LoadScriptCodeAsync(k_InvalidScriptFile, CancellationToken.None))
            .Throws(new ScriptEvaluationException(""));

        m_MockServicesWrapper.Setup(x => x.CliCloudCodeClient)
            .Returns(m_MockCloudCodeClient.Object);
        m_MockServicesWrapper.Setup(x => x.EnvironmentProvider)
            .Returns(m_MockEnvironmentProvider.Object);
        m_MockServicesWrapper.Setup(x => x.CloudCodeInputParser)
            .Returns(m_MockCloudCodeInputParser.Object);
        m_MockServicesWrapper.Setup(x => x.CloudCodeService)
            .Returns(m_MockCloudCodeService.Object);
        m_MockServicesWrapper.Setup(x => x.CloudCodeDeploymentHandler)
            .Returns(m_MockCloudCodeDeploymentHandler.Object);
        m_MockServicesWrapper.Setup(x => x.CliDeploymentOutputHandler)
            .Returns(m_MockCliDeploymentOutputHandler.Object);
        m_MockServicesWrapper.Setup(x => x.CloudCodeScriptsLoader)
            .Returns(m_MockCloudCodeScriptsLoader.Object);


        m_DeploymentService = new CloudCodeDeploymentService(m_MockUnityEnvironment.Object, m_MockServicesWrapper.Object);

        m_MockCloudCodeScriptsLoader.Setup(
            c => c.LoadScriptsAsync(
                k_ValidFilePaths,
                "Cloud Code",
                ".js",
                m_MockCloudCodeInputParser.Object,
                m_MockCloudCodeService.Object,
                m_Contents,
                CancellationToken.None)).ReturnsAsync(new List<IScript>());
    }

    [Test]
    public async Task DeployAsync_CallsLoadFilePathsFromInputCorrectly()
    {
        CloudCodeInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
        };
        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        m_MockDeployFileService.Setup(d => d.ListFilesToDeploy(input.Paths, "*.js"))
            .Returns(k_ValidFilePaths);

        var result = await m_DeploymentService!.Deploy(
            input,
            (StatusContext)null!,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(), Times.Once);
        m_MockCloudCodeClient.Verify(
            x => x.Initialize(
                TestValues.ValidEnvironmentId,
                TestValues.ValidProjectId,
                CancellationToken.None),
            Times.Once);
        m_MockEnvironmentProvider.VerifySet(x => { x.Current = TestValues.ValidEnvironmentId; }, Times.Once);
        m_MockCloudCodeDeploymentHandler.Verify(x => x.DeployAsync(It.IsAny<List<IScript>>()), Times.Once);
        Assert.AreEqual(k_DeployedContents, result.Deployed);
        Assert.AreEqual(k_FailedContents, result.Failed);
    }


    [Test]
    public void DeployAsync_DoesNotThrowOnApiException()
    {
        CloudCodeInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId
        };

        m_MockCloudCodeDeploymentHandler.Setup(ex => ex
            .DeployAsync(It.IsAny<IEnumerable<IScript>>())).ThrowsAsync(new ApiException());
        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        m_MockDeployFileService.Setup(d => d.ListFilesToDeploy(input.Paths, "*.js"))
            .Returns(k_ValidFilePaths);

        Assert.DoesNotThrowAsync(
            () => m_DeploymentService!.Deploy(
                input,
                (StatusContext)null!,
                CancellationToken.None));
    }
}
