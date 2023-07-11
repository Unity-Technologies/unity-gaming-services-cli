using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Export.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Handlers.ImportExport.Scripts;
using Unity.Services.Cli.CloudCode.Model;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.UnitTest.Utils;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using Language = Unity.Services.CloudCode.Authoring.Editor.Core.Model.Language;


namespace Unity.Services.Cli.CloudCode.UnitTest.Handlers;

[TestFixture]
class ExportScriptsHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ICloudCodeService> m_MockCloudCodeService = new();
    readonly Mock<ILogger> m_MockLogger = new();
    readonly Mock<IZipArchiver> m_MockArchiver = new();
    readonly Mock<IFileSystem> m_MockFileSystem = new();
    readonly Mock<ILoadingIndicator> m_MockLoadingIndicator = new();
    readonly static DateTime DateNow = DateTime.Now;
    readonly IEnumerable<ListScriptsResponseResultsInner> m_ScriptsListResponse = new List<ListScriptsResponseResultsInner>()
    {
       new("test1", ScriptType.API, Gateway.CloudCodeApiV1.Generated.Model.Language.JS, true, DateNow, 1),
       new("test2", ScriptType.API, Gateway.CloudCodeApiV1.Generated.Model.Language.JS, true, DateNow, 1),
       new("test3", ScriptType.API, Gateway.CloudCodeApiV1.Generated.Model.Language.JS, true, DateNow, 1),
       new("test4", ScriptType.API, Gateway.CloudCodeApiV1.Generated.Model.Language.JS, true, DateNow, 1),
    };
    readonly IEnumerable<CloudCodeScript> m_Scripts = new List<CloudCodeScript>()
    {
        new(new ScriptName("test1"), Language.JS, "", "{}", new List<CloudCodeParameter>(), DateNow.ToString()),
        new(new ScriptName("test2"), Language.JS, "","{}", new List<CloudCodeParameter>(), DateNow.ToString()),
        new(new ScriptName("test3"), Language.JS, "","{}", new List<CloudCodeParameter>(), DateNow.ToString()),
        new(new ScriptName("test4"), Language.JS, "","{}", new List<CloudCodeParameter>(), DateNow.ToString()),
    };
    CloudCodeScriptsExporter? m_CloudCodeScriptsExporter;

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockCloudCodeService.Reset();
        m_MockLogger.Reset();
        m_MockFileSystem.Reset();
        m_CloudCodeScriptsExporter = new(m_MockCloudCodeService.Object,
            m_MockArchiver.Object,
            m_MockUnityEnvironment.Object,
            m_MockFileSystem.Object,
            m_MockLogger.Object);
    }

    [Test]
    public async Task ExportAsync_CallsLoadingIndicator()
    {
        var exportInput = new ExportInput()
        {
            OutputDirectory = "mock_output_directory"
        };

        await ScriptExportHandler.ExportAsync
        (
            exportInput,
            m_CloudCodeScriptsExporter,
            m_MockLoadingIndicator.Object,
            CancellationToken.None
        );

        m_MockLoadingIndicator.Verify(li => li.StartLoadingAsync(ScriptExportHandler.k_LoadingIndicatorMessage,
            It.IsAny<Func<StatusContext?, Task>>()));
    }

    [Test]
    public async Task ExportAsync_ExportsAndZips()
    {
        var exportInput = new ExportInput()
        {
            OutputDirectory = "mock_output_directory"
        };

        var archivePath = Path.Join(exportInput.OutputDirectory, CloudCodeConstants.JavascriptZipName);

        m_MockFileSystem
            .Setup(x =>
                x.Directory.CreateDirectory(It.IsAny<string>()));

        m_MockFileSystem
            .Setup(x =>
                x.Path.Join(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(archivePath);

        m_MockFileSystem
            .Setup(x =>
                x.File.Exists(It.IsAny<string>()))
            .Returns(false);

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        m_MockCloudCodeService.Setup(
                cs => cs.ListAsync(It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(m_ScriptsListResponse));


        foreach (var script in m_Scripts)
        {
            m_MockCloudCodeService.Setup(
                    cs => cs.GetAsync(It.IsAny<string>(), It.IsAny<string>(), script.Name.ToString(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(
                    new GetScriptResponse(script.Name.ToString(),
                        ScriptType.API,
                        Gateway.CloudCodeApiV1.Generated.Model.Language.JS,
                        activeScript: new GetScriptResponseActiveScript(
                            script.Body,
                            1,
                            DateNow,
                            new List<ScriptParameter>()),
                        new List<GetScriptResponseVersionsInner>(),
                        new List<ScriptParameter>())));
        }

        m_MockFileSystem.Setup(s => s.Directory.CreateDirectory(exportInput.OutputDirectory));
        m_MockFileSystem.Setup(s => s.Path.Join(exportInput.OutputDirectory, CloudCodeConstants.JavascriptZipName))
            .Returns(archivePath);
        m_MockFileSystem.Setup(s => s.File.Exists(archivePath));

        var cancellation = CancellationToken.None;
        await m_CloudCodeScriptsExporter!.ExportAsync(exportInput, cancellation);

        m_MockCloudCodeService.Verify(
            cs => cs.ListAsync(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        m_MockCloudCodeService.Verify(
            cs => cs.GetAsync(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(4));

        m_MockArchiver.Verify(za => za.ZipAsync(
            It.Is<string>(s => s == archivePath),
            It.Is<string>(s => s == CloudCodeConstants.ScriptsEntryName),
            It.Is<IEnumerable<CloudCodeScript>>(s => AssertAreEqual(s.ToList(), m_Scripts.ToList())),
            It.IsAny<CancellationToken>()));
    }

    static bool AssertAreEqual(IList<CloudCodeScript> list1, IList<CloudCodeScript> list2)
    {
        Assert.AreEqual(list1.Count(), list2.Count());
        for (var i = 0; i < list1.Count(); i++)
        {
            Assert.AreEqual(list2[i].Name, list1[i].Name);
            Assert.AreEqual(list2[i].Body, list1[i].Body);
        }

        return true;
    }
}
