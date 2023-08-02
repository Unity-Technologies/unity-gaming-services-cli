using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.CloudCode.Authoring;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.UnitTest.Utils;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest;

[TestFixture]
class JavaScriptFetchServiceTests
{
    readonly Mock<IUnityEnvironment> m_UnityEnvironment = new();
    readonly Mock<IJavaScriptClient> m_Client = new();
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
        SetupLocalResources(out var input, out var scripts, out var files);
        input.CloudProjectId = TestValues.ValidProjectId;
        input.DryRun = dryRun;
        input.Reconcile = reconcile;
        m_UnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        var expectedResult = new FetchResult(
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>());
        m_FetchHandler.Setup(
                x => x.FetchAsync(
                    input.Path,
                    scripts,
                    input.DryRun,
                    input.Reconcile,
                    CancellationToken.None))
            .ReturnsAsync(expectedResult);

        var result = await m_Service.FetchAsync(input, files, null, CancellationToken.None);

        m_Client.Verify(
            x => x.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None),
            Times.Once);
        Assert.That(result.Fetched.Count, Is.EqualTo(expectedResult.Fetched.Count));
        Assert.That(result.Deleted.Count, Is.EqualTo(expectedResult.Deleted.Count));
        Assert.That(result.Created.Count, Is.EqualTo(expectedResult.Created.Count));
        Assert.That(result.Updated.Count, Is.EqualTo(expectedResult.Updated.Count));
        Assert.That(result.Failed.Count, Is.EqualTo(expectedResult.Failed.Count));
    }

    void SetupLocalResources(out FetchInput input, out List<IScript> scripts, out List<string> files)
    {
        input = new FetchInput
        {
            Path = ".",
        };
        files = new List<string>
        {
            "foo.js",
            "bar/foobar.js"
        };
        var filesInstance = new List<string>(files);
        scripts = files.Select(x => new ScriptInfo(ScriptName.FromPath(x)))
            .Cast<IScript>()
            .ToList();
        m_ScriptsLoader.Setup(
                x => x.LoadScriptsAsync(
                    filesInstance,
                    CloudCodeConstants.ServiceType,
                    CloudCodeConstants.JavaScriptFileExtension,
                    m_InputParser.Object,
                    m_ScriptParser.Object,
                    CancellationToken.None))
            .ReturnsAsync(new CloudCodeScriptLoadResult(scripts, new List<IScript>()));
    }

    [Test]
    public async Task GetResourcesFromFilesAsyncReturnsLoadedFiles()
    {
        SetupLocalResources(out var input, out var scripts, out var files);

        var resources = await m_Service.GetResourcesFromFilesAsync(
            files,
            CancellationToken.None);

        Assert.That(resources.LoadedScripts, Is.SameAs(scripts));
    }

    [Test]
    public async Task FailedToLoadAreReported()
    {
        m_ScriptsLoader.Setup(
                x => x.LoadScriptsAsync(
                    It.IsAny<IReadOnlyCollection<string>>(),
                    CloudCodeConstants.ServiceType,
                    CloudCodeConstants.JavaScriptFileExtension,
                    m_InputParser.Object,
                    m_ScriptParser.Object,
                    CancellationToken.None))
            .ReturnsAsync(new CloudCodeScriptLoadResult(new List<IScript>(), new List<IScript>()
            {
                new CloudCodeScript { Name = ScriptName.FromPath("failed-script.js"), Path = "failed-script.js" }
            }));

        var input = new FetchInput
        {
            Path = ".",
        };

        var expectedResult = new FetchResult(
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>());

        m_FetchHandler.Setup(
                x => x.FetchAsync(
                    input.Path,
                    It.IsAny<IReadOnlyList<IScript>>(),
                    input.DryRun,
                    input.Reconcile,
                    CancellationToken.None))
            .ReturnsAsync(expectedResult);

        var actualResult = await m_Service.FetchAsync(
            input,
            new [] { "hello.js" },
            null!,
            CancellationToken.None);

        Assert.IsNotEmpty(actualResult.Failed);
    }
}
