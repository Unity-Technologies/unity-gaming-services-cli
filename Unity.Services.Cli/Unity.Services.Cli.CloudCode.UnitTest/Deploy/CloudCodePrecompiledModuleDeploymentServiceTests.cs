using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.CloudCode.Authoring;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Parameters;
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
        "test_a.ccm",
        "test_b.ccm"
    };

    readonly Mock<ICSharpClient> m_MockCloudCodeClient = new();
    readonly Mock<ICliEnvironmentProvider> m_MockEnvironmentProvider = new();
    readonly Mock<ICloudCodeInputParser> m_MockCloudCodeInputParser = new();
    readonly Mock<ICloudCodeService> m_MockCloudCodeService = new();
    readonly Mock<IDeploymentHandlerWithOutput> m_DeploymentHandlerWithOutput = new();
    readonly Mock<ICloudCodeModulesLoader> m_MockCloudCodeModulesLoader = new();
    static readonly IReadOnlyCollection<DeployContent> k_DeployedContents = new[]
    {
        new DeployContent("module.ccm", "Cloud Code", "path", 100, "Published"),
    };

    static readonly IReadOnlyCollection<DeployContent> k_FailedContents = new[]
    {
        new DeployContent("invalid1.ccm", "Cloud Code", "path", 0, "Failed to Load"),
        new DeployContent("invalid2.ccm", "Cloud Code", "path", 0, "Failed to Load"),
    };

    readonly List<ScriptInfo> m_RemoteContents = new()
    {
        new ScriptInfo("ToDelete", ".ccm")
    };

    readonly List<DeployContent> m_Contents = k_DeployedContents.Concat(k_FailedContents).ToList();

    CloudCodePrecompiledModuleDeploymentService? m_DeploymentService;

    [SetUp]
    public void SetUp()
    {
        m_MockCloudCodeClient.Reset();
        m_MockEnvironmentProvider.Reset();
        m_MockCloudCodeInputParser.Reset();
        m_MockCloudCodeService.Reset();
        m_MockCloudCodeModulesLoader.Reset();
        m_DeploymentHandlerWithOutput.Reset();

        m_DeploymentHandlerWithOutput.SetupGet(c => c.Contents)
            .Returns(m_Contents);

        m_DeploymentService = new CloudCodePrecompiledModuleDeploymentService(
            m_DeploymentHandlerWithOutput.Object,
            m_MockCloudCodeModulesLoader.Object,
            m_MockEnvironmentProvider.Object,
            m_MockCloudCodeClient.Object);

        m_MockCloudCodeModulesLoader.Setup(
                c => c.LoadPrecompiledModulesAsync(
                    k_ValidFilePaths,
                    "Cloud Code Modules",
                    ".ccm",
                    m_Contents))
            .ReturnsAsync(new List<IScript>());
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
            new ScriptName("module.ccm"),
            Language.JS,
            "modules");

        m_MockCloudCodeModulesLoader.Reset();
        m_MockCloudCodeModulesLoader.Setup(
                c => c.LoadPrecompiledModulesAsync(
                    k_ValidFilePaths,
                    "Cloud Code",
                    ".ccm",
                    m_Contents))
            .ReturnsAsync(
                new List<IScript>
                {
                    myModule
                });

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

        List<IScript> testModules = new List<IScript>
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
                    m_Contents))
            .ReturnsAsync(testModules);

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
