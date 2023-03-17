using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.UnitTest.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;
using AuthoringLanguage = Unity.Services.CloudCode.Authoring.Editor.Core.Model.Language;

namespace Unity.Services.Cli.CloudCode.UnitTest.Deploy;

[TestFixture]
public class CloudCodePrecompiledModuleDeploymentServiceTests
{

    static readonly List<string> k_ValidFilePaths = new()
    {
        "test_a.zip",
        "test_b.zip"
    };

    readonly Mock<ICliCloudCodeClient> m_MockCloudCodeClient = new();
    readonly Mock<ICliEnvironmentProvider> m_MockEnvironmentProvider = new();
    readonly Mock<ICloudCodeInputParser> m_MockCloudCodeInputParser = new();
    readonly Mock<ICloudCodeService> m_MockCloudCodeService = new();
    readonly Mock<ICloudCodeDeploymentHandler> m_MockCloudCodeDeploymentHandler = new();
    readonly Mock<ICloudCodeServicesWrapper> m_MockServicesWrapper = new();
    readonly Mock<ICliDeploymentOutputHandler> m_MockCliDeploymentOutputHandler = new();
    readonly Mock<ICloudCodeModulesLoader> m_MockCloudCodeModulesLoader = new();

    static readonly IReadOnlyCollection<DeployContent> k_DeployedContents = new[]
    {
        new DeployContent("module.zip", "Cloud Code", "path", 100, "Published"),
    };

    static readonly IReadOnlyCollection<DeployContent> k_FailedContents = new[]
    {
        new DeployContent("invalid1.zip", "Cloud Code", "path", 0, "Failed to Load"),
        new DeployContent("invalid2.zip", "Cloud Code", "path", 0, "Failed to Load"),
    };

    List<ScriptInfo> m_RemoteContents = new List<ScriptInfo>()
    {
        new ScriptInfo("ToDelete", ".zip")
    };

    List<DeployContent> m_Contents = k_DeployedContents.Concat(k_FailedContents).ToList();

    CloudCodePrecompiledModuleDeploymentService? m_DeploymentService;

    [SetUp]
    public void SetUp()
    {
        m_MockCloudCodeClient.Reset();
        m_MockEnvironmentProvider.Reset();
        m_MockCloudCodeInputParser.Reset();
        m_MockCloudCodeService.Reset();
        m_MockCloudCodeDeploymentHandler.Reset();
        m_MockServicesWrapper.Reset();
        m_MockCliDeploymentOutputHandler.Reset();
        m_MockCloudCodeModulesLoader.Reset();

        m_MockCliDeploymentOutputHandler.SetupGet(c => c.Contents).Returns(m_Contents);

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
        m_MockServicesWrapper.Setup(x => x.CloudCodeModulesLoader)
            .Returns(m_MockCloudCodeModulesLoader.Object);


        m_DeploymentService = new CloudCodePrecompiledModuleDeploymentService(m_MockServicesWrapper.Object);

        m_MockCloudCodeModulesLoader.Setup(
            c => c.LoadPrecompiledModulesAsync(
                k_ValidFilePaths,
                "Cloud Code Modules",
                ".zip",
                m_Contents)
        ).ReturnsAsync(new List<IScript>());
    }

    [Test]
    public async Task DeployAsync_CallsLoadFilePathsFromInputCorrectly()
    {
        CloudCodeInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            Paths = k_ValidFilePaths,
        };

        IScript myModule = new Unity.Services.Cli.CloudCode.Deploy.CloudCodeModule(
            new ScriptName("module.zip"),
            Language.JS,
            "modules");

        m_MockCloudCodeModulesLoader.Reset();
        m_MockCloudCodeModulesLoader.Setup(
            c => c.LoadPrecompiledModulesAsync(
                k_ValidFilePaths,
                "Cloud Code",
                ".zip",
                m_Contents)
        ).ReturnsAsync(new List<IScript>{myModule});

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
        m_MockCloudCodeDeploymentHandler.Verify(x => x.DeployAsync(It.IsAny<List<IScript>>(), false, false), Times.Once);
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

        List<IScript> testModules = new List<IScript>()
        {
            new Unity.Services.Cli.CloudCode.Deploy.CloudCodeModule(
                new ScriptName("module.ccm"),
                Language.JS,
                "modules"),
            new Unity.Services.Cli.CloudCode.Deploy.CloudCodeModule(
                new ScriptName("module2.ccm"),
                Language.JS,
                "modules")
        };

        m_MockCloudCodeModulesLoader.Reset();
        m_MockCloudCodeModulesLoader.Setup(
            c => c.LoadPrecompiledModulesAsync(
                k_ValidFilePaths,
                "Cloud Code",
                ".ccm",
                m_Contents)
        ).ReturnsAsync(testModules);

        m_MockCloudCodeDeploymentHandler.Setup(ex => ex
                .DeployAsync(It.IsAny<IEnumerable<IScript>>(), true, false))
            .Returns(Task.FromResult(new DeployResult(
                System.Array.Empty<IScript>(),
                System.Array.Empty<IScript>(),
                m_RemoteContents.Select(script=>(IScript)script).ToList(),
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
            result.Deleted.Any(item=>
                m_RemoteContents.Any(content=> content.Name.ToString() == item.Name)));
    }


    [Test]
    public void DeployAsync_DoesNotThrowOnApiException()
    {
        CloudCodeInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId
        };

        m_MockCloudCodeDeploymentHandler.Setup(ex => ex
            .DeployAsync(It.IsAny<IEnumerable<IScript>>(), false, false)).ThrowsAsync(new ApiException());

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
