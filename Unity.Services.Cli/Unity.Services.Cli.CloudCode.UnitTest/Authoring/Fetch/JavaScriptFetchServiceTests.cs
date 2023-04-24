using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.CloudCode.Authoring;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.UnitTest.Utils;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest;

[TestFixture]
class JavaScriptFetchServiceTests
{
    readonly Mock<IUnityEnvironment> m_UnityEnvironment = new();
    readonly Mock<IJavaScriptClient> m_Client = new();
    readonly Mock<IDeployFileService> m_DeployFileService = new();
    readonly Mock<ICloudCodeScriptsLoader> m_ScriptsLoader = new();
    readonly Mock<ICloudCodeInputParser> m_InputParser = new();
    readonly Mock<ICloudCodeScriptParser> m_ScriptParser = new();
    readonly Mock<IJavaScriptFetchHandler> m_FetchHandler = new();

    readonly JavaScriptFetchService m_Service;

    public JavaScriptFetchServiceTests()
    {
        m_Service = new JavaScriptFetchService(
            m_UnityEnvironment.Object,
            m_Client.Object,
            m_DeployFileService.Object,
            m_ScriptsLoader.Object,
            m_InputParser.Object,
            m_ScriptParser.Object,
            m_FetchHandler.Object);
    }

    [SetUp]
    public void SetUp()
    {
        m_UnityEnvironment.Reset();
        m_Client.Reset();
        m_DeployFileService.Reset();
        m_ScriptsLoader.Reset();
        m_InputParser.Reset();
        m_ScriptParser.Reset();
        m_FetchHandler.Reset();
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task FetchAsyncInitializesClientAndGetsResultFromHandler(bool dryRun, bool reconcile)
    {
        SetupLocalResources(out var input, out var scripts);
        input.CloudProjectId = TestValues.ValidProjectId;
        input.DryRun = dryRun;
        input.Reconcile = reconcile;
        m_UnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        var expectedResult = new FetchResult(
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>());
        m_FetchHandler.Setup(
                x => x.FetchAsync(input.Path, scripts, input.DryRun, input.Reconcile, CancellationToken.None))
            .ReturnsAsync(expectedResult);

        var result = await m_Service.FetchAsync(input, null, CancellationToken.None);

        m_Client.Verify(
            x => x.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None),
            Times.Once);
        Assert.That(result, Is.SameAs(expectedResult));
    }

    void SetupLocalResources(out FetchInput input, out List<IScript> scripts)
    {
        input = new FetchInput
        {
            Path = ".",
        };
        var files = new[]
        {
            "foo.js",
            "bar/foobar.js"
        };
        scripts = files.Select(x => new ScriptInfo(ScriptName.FromPath(x)))
            .Cast<IScript>()
            .ToList();
        m_DeployFileService.Setup(
                x => x.ListFilesToDeploy(
                    It.IsAny<IReadOnlyList<string>>(),
                    Constants.JavaScriptFileExtension))
            .Returns(files);
        m_ScriptsLoader.Setup(
                x => x.LoadScriptsAsync(
                    files,
                    Constants.ServiceType,
                    Constants.JavaScriptFileExtension,
                    m_InputParser.Object,
                    m_ScriptParser.Object,
                    It.IsAny<ICollection<DeployContent>>(),
                    CancellationToken.None))
            .ReturnsAsync(new CloudCodeScriptLoadResult(scripts, new List<DeployContent>()));
    }

    [Test]
    public async Task GetResourcesFromFilesAsyncReturnsLoadedFiles()
    {
        SetupLocalResources(out var input, out var scripts);

        var resources = await m_Service.GetResourcesFromFilesAsync(input, CancellationToken.None);

        Assert.That(resources.LoadedScripts, Is.SameAs(scripts));
    }
}
