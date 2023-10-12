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
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Export.Input;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Handlers.ImportExport.Modules;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.UnitTest.Utils;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using AuthoringLanguage = Unity.Services.CloudCode.Authoring.Editor.Core.Model.Language;
using Language = Unity.Services.Gateway.CloudCodeApiV1.Generated.Model.Language;
using Module = Unity.Services.Cli.CloudCode.Deploy.CloudCodeModule;

namespace Unity.Services.Cli.CloudCode.UnitTest.Handlers;

[TestFixture]
class ExportModulesHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<IFileSystem> m_MockFileSystem = new();
    readonly Mock<ICloudCodeService> m_MockCloudCodeService = new();
    readonly Mock<ICloudCodeModulesDownloader> m_MockCloudCodeModulesDownloader = new();
    readonly Mock<ILogger> m_MockLogger = new();
    readonly Mock<IZipArchiver> m_MockArchiver = new();
    readonly Mock<ILoadingIndicator> m_MockLoadingIndicator = new();
    readonly static DateTime DateNow = DateTime.Now;
    readonly IEnumerable<ListModulesResponseResultsInner> m_ModulesListResponse = new List<ListModulesResponseResultsInner>()
    {
       new("test1",  Language.JS, new Dictionary<string, string>(), "url", DateNow),
       new("test2", Language.JS, new Dictionary<string, string>(), "url", DateNow),
       new("test3",  Language.JS, new Dictionary<string, string>(),"url", DateNow),
    };

    readonly IEnumerable<Module> m_Modules = new List<Module>()
    {
        new(new ScriptName("test1"), AuthoringLanguage.JS, "test1","{}", new List<CloudCodeParameter>(), DateNow.ToString()),
        new(new ScriptName("test2"), AuthoringLanguage.JS, "test2","{}", new List<CloudCodeParameter>(), DateNow.ToString()),
        new(new ScriptName("test3"), AuthoringLanguage.JS, "test3","{}", new List<CloudCodeParameter>(), DateNow.ToString()),
    };

    CloudCodeModulesExporter? m_CloudCodeModulesExporter;

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockCloudCodeService.Reset();
        m_MockLogger.Reset();
        m_MockCloudCodeModulesDownloader.Reset();
        m_CloudCodeModulesExporter = new(m_MockCloudCodeService.Object,
            m_MockCloudCodeModulesDownloader.Object,
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

        await ModuleExportHandler.ExportAsync
        (
            exportInput,
            m_CloudCodeModulesExporter,
            m_MockLoadingIndicator.Object,
            CancellationToken.None
        );

        m_MockLoadingIndicator.Verify(li => li.StartLoadingAsync(ModuleExportHandler.k_LoadingIndicatorMessage,
            It.IsAny<Func<StatusContext?, Task>>()));
    }

    [Test]
    [Ignore("This is not a unit test and must be moved")]
    public async Task ExportAsync_ExportsAndZips()
    {
        var exportInput = new ExportInput()
        {
            FileName = CloudCodeConstants.ZipNameModules,
            OutputDirectory = "test_output_directory"
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        m_MockCloudCodeService.Setup(
                cs => cs.ListModulesAsync(It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(m_ModulesListResponse));

        m_MockCloudCodeModulesDownloader.Setup(
            cs => cs.DownloadModule(
                It.Is<Module>(m => m.Name.ToString() == "Module"),
                It.IsAny<CancellationToken>()))
             .ReturnsAsync(File.OpenRead("ModuleTestCases\\Module.ccm"));

        m_MockFileSystem.Setup(s => s.Directory.CreateDirectory(exportInput.OutputDirectory));
        m_MockFileSystem.Setup(s => s.Path.Join(exportInput.OutputDirectory, exportInput.FileName))
            .Returns("test_output_directory\\ugs.ccmzip");
        m_MockFileSystem.Setup(s => s.File.Exists("test_output_directory\\ugs.ccmzip"));

        await m_CloudCodeModulesExporter!.ExportAsync(exportInput, CancellationToken.None);
        m_MockCloudCodeService.Verify(
            cs => cs.ListModulesAsync(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var archivePath = Path.Join(exportInput.OutputDirectory, exportInput.FileName);
        m_MockArchiver.Verify(za => za.ZipAsync(
            archivePath,
            CloudCodeConstants.EntryNameModules,
            It.Is<IEnumerable<Module>>(m => m.Count() == m_Modules.Count()),
            It.IsAny<CancellationToken>()));
    }
}
