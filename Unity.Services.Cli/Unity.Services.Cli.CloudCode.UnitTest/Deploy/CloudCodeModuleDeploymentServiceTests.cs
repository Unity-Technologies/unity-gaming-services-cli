using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using Unity.Services.CloudCode.Authoring.Editor.Core.IO;
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

    static readonly List<string> k_ValidSlnFilePaths = new()
    {
        "test_sln_a.sln"
    };

    readonly Mock<ICSharpClient> m_MockCloudCodeClient = new();
    readonly Mock<ICliEnvironmentProvider> m_MockEnvironmentProvider = new();
    readonly Mock<ICloudCodeInputParser> m_MockCloudCodeInputParser = new();
    readonly Mock<ICloudCodeService> m_MockCloudCodeService = new();
    readonly Mock<ICloudCodeDeploymentHandler> m_DeploymentHandler = new();
    readonly Mock<ICloudCodeModulesLoader> m_MockCloudCodeModulesLoader = new();
    readonly Mock<IDeployFileService> m_MockDeployFileService = new();
    readonly Mock<ISolutionPublisher> m_MockSolutionPublisher = new();
    readonly Mock<IModuleZipper> m_MockModuleZipper = new();
    readonly Mock<IFileSystem> m_MockFileSystem = new();

    static readonly IReadOnlyList<CloudCodeModuleScript> k_DeployedContents = new[]
    {
        new CloudCodeModuleScript(
            "module.ccm",
            "path",
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
            m_MockDeployFileService.Object,
            m_MockSolutionPublisher.Object,
            m_MockModuleZipper.Object,
            m_MockFileSystem.Object);

        m_MockCloudCodeModulesLoader.Setup(
                c => c.LoadPrecompiledModulesAsync(
                    k_ValidCcmFilePaths,
                    CloudCodeConstants.ServiceTypeModules))
            .ReturnsAsync(k_DeployedContents.OfType<IScript>().ToList());
    }

    [Test]
    public async Task DeployAsync_RemovesDuplicatesBeforeDeploy()
    {
        var outputCcmPath = "test_a.ccm";

        CloudCodeInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            Paths = k_ValidSlnFilePaths,
        };

        m_MockCloudCodeModulesLoader.Reset();

        m_MockDeployFileService.Setup(
                c => c.ListFilesToDeploy(
                    It.IsAny<List<string>>(),
                    CloudCodeConstants.FileExtensionModulesCcm,
                    false))
            .Returns(k_ValidCcmFilePaths);

        m_MockDeployFileService.Setup(
                c => c.ListFilesToDeploy(
                    It.IsAny<List<string>>(),
                    CloudCodeConstants.FileExtensionModulesSln,
                    false))
            .Returns(k_ValidSlnFilePaths);

        var fakeModuleName = "FakeModuleName";
        var testSlnDirName = "FakeSolutionDirName";
        var slnPath = "FakeSolutionPath";

        m_MockSolutionPublisher.Setup(
                x => x.PublishToFolder(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeModuleName);

        m_MockFileSystem.Setup(
            x => x.GetDirectoryName(It.IsAny<string>()));
        m_MockFileSystem.Setup(
                x => x.GetFullPath(It.IsAny<string>()))
            .Returns(testSlnDirName);

        m_MockFileSystem.Setup(
                x => x.Combine(testSlnDirName, CloudCodeModuleDeploymentService.OutputPath))
            .Returns(slnPath);

        m_MockModuleZipper.Setup(
                x => x.ZipCompilation(
                    It.IsAny<string>(),
                    fakeModuleName,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(outputCcmPath);

        await m_DeploymentService!.Deploy(
            input,
            k_ValidCcmFilePaths,
            TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            null!,
            CancellationToken.None);

        var slnName = Path.GetFileNameWithoutExtension(k_ValidSlnFilePaths.First());
        var dllOutputPath = Path.Combine(Path.GetTempPath(), slnName);
        var moduleCompilationPath = Path.Combine(dllOutputPath, "module-compilation");

        m_MockSolutionPublisher.Verify(
            x => x.PublishToFolder(
                k_ValidSlnFilePaths.First(),
                moduleCompilationPath,
                It.IsAny<CancellationToken>()),
            Times.Once);

        m_MockCloudCodeModulesLoader.Verify(
            x => x.LoadPrecompiledModulesAsync(
                k_ValidCcmFilePaths,
                It.IsAny<string>()),
            Times.Once);
    }

    [Test]
    public async Task DeployAsync_GenerateSolutionFromSlnInput()
    {
        var outputCcmPath = "test_result.ccm";

        CloudCodeInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            Paths = k_ValidSlnFilePaths,
        };

        m_MockCloudCodeModulesLoader.Reset();

        m_MockDeployFileService.Setup(
                c => c.ListFilesToDeploy(
                    It.IsAny<List<string>>(),
                    CloudCodeConstants.FileExtensionModulesCcm,
                    false))
            .Returns(k_ValidCcmFilePaths);

        m_MockDeployFileService.Setup(
                c => c.ListFilesToDeploy(
                    It.IsAny<List<string>>(),
                    CloudCodeConstants.FileExtensionModulesSln,
                    false))
            .Returns(k_ValidSlnFilePaths);

        var fakeModuleName = "FakeModuleName";
        var testSlnDirName = "FakeSolutionDirName";
        var slnPath = "FakeSolutionPath";

        m_MockSolutionPublisher.Setup(
                x => x.PublishToFolder(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeModuleName);

        m_MockFileSystem.Setup(
            x => x.GetDirectoryName(It.IsAny<string>()));
        m_MockFileSystem.Setup(
                x => x.GetFullPath(It.IsAny<string>()))
            .Returns(testSlnDirName);
        m_MockFileSystem.Setup(
                x => x.Combine(testSlnDirName, CloudCodeModuleDeploymentService.OutputPath))
            .Returns(slnPath);

        m_MockModuleZipper.Setup(
                x => x.ZipCompilation(
                    It.IsAny<string>(),
                    fakeModuleName,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(outputCcmPath);

        await m_DeploymentService!.Deploy(
            input,
            k_ValidCcmFilePaths,
            TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            null!,
            CancellationToken.None);

        var slnName = Path.GetFileNameWithoutExtension(k_ValidSlnFilePaths.First());
        var dllOutputPath = Path.Combine(Path.GetTempPath(), slnName);
        var moduleCompilationPath = Path.Combine(dllOutputPath, "module-compilation");

        m_MockSolutionPublisher.Verify(
            x => x.PublishToFolder(
                k_ValidSlnFilePaths.First(),
                moduleCompilationPath,
                It.IsAny<CancellationToken>()),
            Times.Once);

        var resultList = new List<string>();
        resultList.AddRange(k_ValidCcmFilePaths);
        resultList.Add(outputCcmPath);
        m_MockCloudCodeModulesLoader.Verify(
            x => x.LoadPrecompiledModulesAsync(
                resultList,
                It.IsAny<string>()),
            Times.Once);
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
        m_MockCloudCodeModulesLoader.Setup(
                c => c.LoadPrecompiledModulesAsync(
                    k_ValidCcmFilePaths,
                    It.IsAny<string>()))
            .ReturnsAsync(loadedResult);

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
        Assert.AreEqual(k_DeployedContents, result.Deployed);
        Assert.AreEqual(k_FailedContents, result.Failed);
    }

    [Test]
    public async Task DeployAsync_FailsGeneration()
    {
        CloudCodeInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            Paths = k_ValidSlnFilePaths,
        };

        m_MockCloudCodeModulesLoader.Reset();

        m_MockDeployFileService.Setup(
                c => c.ListFilesToDeploy(
                    It.IsAny<List<string>>(),
                    CloudCodeConstants.FileExtensionModulesCcm,
                    false))
            .Returns(k_ValidCcmFilePaths);

        m_MockDeployFileService.Setup(
                c => c.ListFilesToDeploy(
                    It.IsAny<List<string>>(),
                    CloudCodeConstants.FileExtensionModulesSln,
                    false))
            .Returns(k_ValidSlnFilePaths);

        var testFakeModuleName = "FakeModuleName";
        var testSlnDirName = "FakeSolutionDirName";
        var slnPath = "FakeSolutionPath";

        m_MockSolutionPublisher.Setup(
                x => x.PublishToFolder(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(testFakeModuleName);
        m_MockFileSystem.Setup(
            x => x.GetDirectoryName(It.IsAny<string>()));
        m_MockFileSystem.Setup(
                x => x.GetFullPath(It.IsAny<string>()))
            .Returns(testSlnDirName);
        m_MockFileSystem.Setup(
                x => x.Combine(testSlnDirName, CloudCodeModuleDeploymentService.OutputPath))
            .Returns(slnPath);

        m_MockModuleZipper.Setup(
                x => x.ZipCompilation(
                    It.IsAny<string>(),
                    testFakeModuleName,
                    It.IsAny<CancellationToken>()))
            .Throws(new Exception("Fake Exception"));

        IScript myModule = new Unity.Services.Cli.CloudCode.Deploy.CloudCodeModule(
            new ScriptName("module.ccm"),
            Language.JS,
            "modules");

        var loadedResult = new List<IScript>
        {
            myModule
        };
        m_MockCloudCodeModulesLoader.Setup(
                c => c.LoadPrecompiledModulesAsync(
                    It.IsAny<List<string>>(),
                    It.IsAny<string>()))
            .ReturnsAsync(loadedResult);

        var result = await m_DeploymentService!.Deploy(
            input,
            k_ValidCcmFilePaths,
            TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            null!,
            CancellationToken.None);

        Assert.AreEqual(result.Failed.Count, k_FailedContents.Count + 1);
        Assert.IsTrue(result.Failed.Any(x => x.Name == k_ValidSlnFilePaths.First()));
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
                c => c.LoadPrecompiledModulesAsync(
                    k_ValidCcmFilePaths,
                    CloudCodeConstants.ServiceTypeScripts))
            .ReturnsAsync(testModules.OfType<IScript>().ToList());

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
