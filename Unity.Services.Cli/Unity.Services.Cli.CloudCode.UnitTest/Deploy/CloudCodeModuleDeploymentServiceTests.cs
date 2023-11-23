using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.CloudCode.Authoring;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.UnitTest.Utils;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;
using AuthoringLanguage = Unity.Services.CloudCode.Authoring.Editor.Core.Model.Language;
using CloudCodeModuleScript = Unity.Services.Cli.CloudCode.Deploy.CloudCodeModule;

namespace Unity.Services.Cli.CloudCode.UnitTest.Deploy;

[TestFixture]
public class CloudCodeModuleDeploymentServiceTests
{
    static readonly List<string> k_ValidCcmFilePaths = new()
    {
        "test_a.ccm",
        "test_b.ccm"
    };

    readonly Mock<ICSharpClient> m_MockCloudCodeClient = new();
    readonly Mock<ICliEnvironmentProvider> m_MockEnvironmentProvider = new();
    readonly Mock<ICloudCodeInputParser> m_MockCloudCodeInputParser = new();
    readonly Mock<ICloudCodeService> m_MockCloudCodeService = new();
    readonly Mock<ICloudCodeDeploymentHandler> m_DeploymentHandler = new();
    readonly Mock<ICloudCodeModulesLoader> m_MockCloudCodeModulesLoader = new();
    readonly Mock<IDeployFileService> m_MockDeployFileService = new();
    readonly Mock<ISolutionPublisher> m_MockSolutionPublisher = new();

    static readonly IReadOnlyList<CloudCodeModuleScript> k_DeployedContents = new[]
    {
        new CloudCodeModuleScript(
            "module.ccm",
            "path/module.ccm",
            100,
            DeploymentStatus.UpToDate)
    };

    static readonly IReadOnlyList<CloudCodeModuleScript> k_FailedContents = new[]
    {
        new CloudCodeModuleScript(
            "invalid1.ccm",
            "path",
            0,
            DeploymentStatus.Empty),
        new CloudCodeModuleScript(
            "invalid2.ccm",
            "path",
            0,
            DeploymentStatus.Empty)
    };

    readonly List<ScriptInfo> m_RemoteContents = new()
    {
        new ScriptInfo("ToDelete", ".ccm")
    };

    CloudCodeModuleDeploymentService? m_DeploymentService;

    [SetUp]
    public void SetUp()
    {
        m_MockCloudCodeClient.Reset();
        m_MockEnvironmentProvider.Reset();
        m_MockCloudCodeInputParser.Reset();
        m_MockCloudCodeService.Reset();
        m_MockCloudCodeModulesLoader.Reset();
        m_DeploymentHandler.Reset();
        m_MockSolutionPublisher.Reset();

        m_DeploymentHandler.Setup(
                c => c.DeployAsync(
                    It.IsAny<IEnumerable<IScript>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
            .Returns(
                Task.FromResult(
                    new DeployResult(
                        new List<IScript>(),
                        new List<IScript>(),
                        new List<IScript>(),
                        k_DeployedContents,
                        k_FailedContents)));

        m_DeploymentService = new CloudCodeModuleDeploymentService(
            m_DeploymentHandler.Object,
            m_MockCloudCodeModulesLoader.Object,
            m_MockEnvironmentProvider.Object,
            m_MockCloudCodeClient.Object,
            m_MockDeployFileService.Object);

        m_MockCloudCodeModulesLoader.Setup(
                c => c.LoadModulesAsync(
                    k_ValidCcmFilePaths,
                    It.IsAny<List<string>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                () =>
                {
                    return (k_DeployedContents.OfType<IScript>().ToList(), new List<IScript>());
                });
    }

    [Test]
    public async Task DeployAsync_CallsFullPathCorrectly()
    {
        CloudCodeInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            Paths = k_ValidCcmFilePaths,
        };

        IScript myModule = new Unity.Services.Cli.CloudCode.Deploy.CloudCodeModule(
            new ScriptName("module.ccm"),
            Language.JS,
            "modules");

        m_MockCloudCodeModulesLoader.Reset();

        m_MockDeployFileService.Setup(
                c => c.ListFilesToDeploy(
                    k_ValidCcmFilePaths,
                    CloudCodeConstants.FileExtensionModulesCcm,
                    false))
            .Returns(k_ValidCcmFilePaths);

        m_MockDeployFileService.Setup(
                c => c.ListFilesToDeploy(
                    It.IsAny<List<string>>(),
                    CloudCodeConstants.FileExtensionModulesSln,
                    false))
            .Returns(new Collection<string>());

        var loadedResult = new List<IScript>
        {
            myModule
        };
        m_MockCloudCodeModulesLoader
            .Setup(
                c => c.LoadModulesAsync(
                    k_ValidCcmFilePaths,
                    It.IsAny<List<string>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                () =>
                {
                    return (loadedResult, new List<IScript>());
                });

        var result = await m_DeploymentService!.Deploy(
            input,
            k_ValidCcmFilePaths,
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
        m_DeploymentHandler.Verify(x => x.DeployAsync(loadedResult, false, false), Times.Once);

        Assert.AreEqual(k_DeployedContents.First().Name.ToString(), result.Deployed.First().Name);
        Assert.AreEqual(k_DeployedContents.First().Path, result.Deployed.First().Path);
        Assert.AreEqual(k_FailedContents.Count, result.Failed.Count);
    }

    [Test]
    public async Task DeployReconcileAsync_WillCreateDeleteContent()
    {
        CloudCodeInput input = new()
        {
            Reconcile = true,
            CloudProjectId = TestValues.ValidProjectId,
        };

        var testModules = new[]
        {
            new CloudCodeModuleScript(
                new ScriptName("module.ccm"),
                Language.JS,
                "modules"),
            new CloudCodeModuleScript(
                new ScriptName("module2.ccm"),
                Language.JS,
                "modules")
        };

        m_MockCloudCodeModulesLoader.Reset();
        m_MockCloudCodeModulesLoader.Setup(
                c => c.LoadModulesAsync(
                    k_ValidCcmFilePaths,
                    It.IsAny<List<string>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                () =>
                {
                    return (testModules.OfType<IScript>().ToList(), new List<IScript>());
                });

        m_MockDeployFileService.Setup(
                c => c.ListFilesToDeploy(
                    k_ValidCcmFilePaths,
                    CloudCodeConstants.FileExtensionModulesCcm,
                    false))
            .Returns(k_ValidCcmFilePaths);

        m_MockDeployFileService.Setup(
                c => c.ListFilesToDeploy(
                    k_ValidCcmFilePaths,
                    CloudCodeConstants.FileExtensionModulesSln,
                    false))
            .Returns(new Collection<string>());

        m_DeploymentHandler.Setup(
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
            k_ValidCcmFilePaths,
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

        m_DeploymentHandler.Setup(
                ex => ex.DeployAsync(It.IsAny<IEnumerable<IScript>>(), false, false))
            .ThrowsAsync(new ApiException());

        m_MockDeployFileService.Setup(
                c => c.ListFilesToDeploy(
                    k_ValidCcmFilePaths,
                    CloudCodeConstants.FileExtensionModulesCcm,
                    false))
            .Returns(k_ValidCcmFilePaths);

        m_MockDeployFileService.Setup(
                c => c.ListFilesToDeploy(
                    k_ValidCcmFilePaths,
                    CloudCodeConstants.FileExtensionModulesSln,
                    false))
            .Returns(new Collection<string>());

        Assert.DoesNotThrowAsync(
            () => m_DeploymentService!.Deploy(
                input,
                k_ValidCcmFilePaths,
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                null!,
                CancellationToken.None));
    }
}
