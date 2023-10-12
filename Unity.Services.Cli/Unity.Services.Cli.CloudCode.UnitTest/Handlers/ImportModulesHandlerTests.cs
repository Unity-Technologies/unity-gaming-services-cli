using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Import.Input;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Handlers.ImportExport.Modules;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.UnitTest.Utils;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using AuthoringLanguage = Unity.Services.CloudCode.Authoring.Editor.Core.Model.Language;
using Language = Unity.Services.Gateway.CloudCodeApiV1.Generated.Model.Language;
using Module = Unity.Services.Cli.CloudCode.Deploy.CloudCodeModule;

namespace Unity.Services.Cli.CloudCode.UnitTest.Handlers;

enum ModuleApiCallType
{
    CreateOrUpdate,
    Delete,
    Get,
    List
}

[TestFixture]
class ImportModuleHandlerTests
{
    const string k_ImportTestFileDirectory
        = "ModuleTestCases";
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ICloudCodeService> m_MockCloudCodeService = new();
    readonly Mock<ICloudCodeModulesDownloader> m_MockCloudCodeModulesDownloader = new();
    readonly Mock<ICloudCodeInputParser> m_MockCloudCodeInputParser = new();
    readonly Mock<ILogger> m_MockLogger = new();
    readonly Mock<IZipArchiver> m_MockArchiver = new();
    readonly Mock<ILoadingIndicator> m_MockLoadingIndicator = new();

    readonly static DateTime DateNow = DateTime.Now;

    readonly IEnumerable<ListModulesResponseResultsInner> m_ModulesListSingleModuleResponse =
        new List<ListModulesResponseResultsInner>()
        {
            new("test1", Language.JS, new Dictionary<string, string>(), "url", DateNow, DateNow),
        };


    readonly Module m_MockNonDuplicateModule = new(new ScriptName("test"), AuthoringLanguage.JS, "test_3.ccm", "{}",
        new List<CloudCodeParameter>(), DateNow.ToString());

    // This mock script updates the existing script in m_MockModules if used within tests
    readonly Module m_MockModule = new(new ScriptName("test1"), AuthoringLanguage.JS, "test_3.ccm", "{}",
        new List<CloudCodeParameter>(), DateNow.ToString());

    readonly List<Module> m_MockModules = new()
    {
        new(new ScriptName("test1"), AuthoringLanguage.JS, "path", "{}", new List<CloudCodeParameter>(),
            DateNow.ToString()),
        new(new ScriptName("test2"), AuthoringLanguage.JS, "path", "{}", new List<CloudCodeParameter>(),
            DateNow.ToString()),
        new(new ScriptName("test3"), AuthoringLanguage.JS, "path", "{}", new List<CloudCodeParameter>(),
            DateNow.ToString()),
        new(new ScriptName("test4"), AuthoringLanguage.JS, "path", "{}", new List<CloudCodeParameter>(),
            DateNow.ToString()),
    };

    CloudCodeModulesImporter? m_CloudCodeModulesImporter;


    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockCloudCodeService.Reset();
        m_MockCloudCodeInputParser.Reset();
        m_MockLogger.Reset();
        m_MockCloudCodeModulesDownloader.Reset();
        m_MockArchiver.Setup(
                za => za.UnzipAsync<Module>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(m_MockModules);

        m_CloudCodeModulesImporter = new CloudCodeModulesImporter(
            m_MockCloudCodeService.Object,
            m_MockArchiver.Object,
            m_MockUnityEnvironment.Object,
            m_MockLogger.Object);

        var mockStream = new MemoryStream(new byte[] { 0x42 });
        m_MockArchiver.Setup(
                za => za.GetEntry(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(new ZipEntryStream(mockStream));
    }

    [Test]
    public async Task ImportAsync_CallsLoadingIndicator()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory"
        };

        await ModulesImportHandler.ImportAsync(
            importInput,
            m_CloudCodeModulesImporter,
            m_MockLoadingIndicator.Object,
            CancellationToken.None
        );

        m_MockLoadingIndicator.Verify(li => li.StartLoadingAsync(ModulesImportHandler.k_LoadingIndicatorMessage,
            It.IsAny<Func<StatusContext?, Task>>()));
    }

    [Test]
    public async Task ImportAsync_Unzips()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory",
            DryRun = true
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await ImportInternalAsync(importInput);

        var archivePath = Path.Join(importInput.InputDirectory, CloudCodeConstants.ZipNameModules);

        m_MockArchiver.Verify(za => za.UnzipAsync<Module>(
            archivePath,
            CloudCodeConstants.EntryNameModules,
            It.IsAny<CancellationToken>()));
    }

    [Test]
    public async Task ImportAsync_DryRunDoesNotImport()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory",
            DryRun = true
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await ImportInternalAsync(importInput);

        VerifyApiCalls(new List<ModuleApiCallType>
        {
            ModuleApiCallType.List
        });
    }

    [Test]
    public void ThrowsWhenModulePathIsEmpty()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory",
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        SetupList(m_ModulesListSingleModuleResponse, new List<Module>() { m_MockModule });
        SetupCreateOrUpdate();

        m_MockArchiver.Setup(
                za => za.UnzipAsync<Module>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Module>()
            {
                new(new ScriptName("test1"), AuthoringLanguage.JS, "", "{}",
                    new List<CloudCodeParameter>(), DateNow.ToString())
            });


        Assert.ThrowsAsync<AggregateException>(async () => await ImportInternalAsync(importInput));

        VerifyApiCalls(new List<ModuleApiCallType>()
        {
            ModuleApiCallType.List,
        });
    }

    [Test]
    public async Task ModuleExists_Updates()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "ModuleTestCases",
            FileName = "test.ccmzip"
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        SetupList(m_ModulesListSingleModuleResponse, new List<Module>() { m_MockModule });
        SetupCreateOrUpdate();

        m_MockArchiver.Setup(
                za => za.UnzipAsync<Module>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Module>()
            {
                m_MockModule
            });

        await ImportInternalAsync(importInput);

        VerifyApiCalls(new List<ModuleApiCallType>()
        {
            ModuleApiCallType.List,
            ModuleApiCallType.CreateOrUpdate
        });
    }

    [Test]
    public void ModuleUpdate_Fails()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "ModuleTestCases",
            FileName = "test.ccmzip"
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        SetupList(m_ModulesListSingleModuleResponse, new List<Module>() { m_MockModule });
        SetupCreateOrUpdate(true);

        m_MockArchiver.Setup(
                za => za.UnzipAsync<Module>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Module>()
            {
                m_MockModule
            });

        Assert.ThrowsAsync<AggregateException>(async () => await ImportInternalAsync(importInput));

        VerifyApiCalls(new List<ModuleApiCallType>()
        {
            ModuleApiCallType.List,
            ModuleApiCallType.CreateOrUpdate
        });
    }

    [Test]
    public async Task ConfigDoesNotExist_Creates()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "ModuleTestCases",
            FileName = "test.ccmzip",
        };

        m_MockArchiver.Setup(
                za => za.UnzipAsync<Module>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Module>()
            {
                m_MockModule
            });

        var mockStream = new MemoryStream(new byte[] { 0x42 });

        m_MockArchiver.Setup(
                za => za.GetEntry(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(new ZipEntryStream(mockStream));


        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        SetupGet(null!, true);
        SetupList(new List<ListModulesResponseResultsInner>(), new List<Module>());
        SetupCreateOrUpdate();

        await ImportInternalAsync(importInput);

        VerifyApiCalls(new List<ModuleApiCallType>()
        {
            ModuleApiCallType.CreateOrUpdate,
            ModuleApiCallType.List
        });
    }

    [Test]
    public void ConfigDoesNotExist_CreateFails()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = k_ImportTestFileDirectory,
            FileName = "test.ccmzip"
        };

        var dir = Directory.GetCurrentDirectory();
        m_MockArchiver.Setup(
                za => za.UnzipAsync<Module>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Module>()
            {
                m_MockModule
            });

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        SetupGet(null!, true);
        SetupList(new List<ListModulesResponseResultsInner>(), new List<Module>());
        SetupCreateOrUpdate(true);

        Assert.ThrowsAsync<AggregateException>(async () => await ImportInternalAsync(importInput));

        VerifyApiCalls(new List<ModuleApiCallType>()
        {
            ModuleApiCallType.CreateOrUpdate,
            ModuleApiCallType.List
        });
    }

    [Test]
    public async Task Reconcile_Deletes()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "ModuleTestCases",
            FileName = "test.ccmzip",
            Reconcile = true
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        m_MockArchiver.Setup(
                za => za.UnzipAsync<Module>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Module>()
            {
                m_MockNonDuplicateModule
            });

        SetupList(new List<ListModulesResponseResultsInner>()
        {
            new(m_MockNonDuplicateModule.Name.ToString(), Language.JS, new Dictionary<string, string>(), "url", DateNow, DateNow)
        }, new List<Module>() { m_MockNonDuplicateModule });

        SetupDelete();

        SetupList(m_ModulesListSingleModuleResponse, new List<Module>() { m_MockModule });

        SetupCreateOrUpdate();

        await ImportInternalAsync(importInput);

        VerifyApiCalls(new List<ModuleApiCallType>()
        {
            ModuleApiCallType.List,
            ModuleApiCallType.Delete,
            ModuleApiCallType.CreateOrUpdate
        });
    }

    [Test]
    public void Reconcile_Delete_Throws()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory",
            Reconcile = true
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        m_MockArchiver.Setup(
                za => za.UnzipAsync<Module>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Module>()
            {
                m_MockNonDuplicateModule
            });

        SetupList(new List<ListModulesResponseResultsInner>()
        {
            new(m_MockNonDuplicateModule.Name.ToString(), Language.JS, new Dictionary<string, string>(), "url", DateNow, DateNow)
        }, new List<Module>() { m_MockNonDuplicateModule });

        SetupDelete(true);

        SetupList(m_ModulesListSingleModuleResponse, new List<Module>() { m_MockModule });

        SetupCreateOrUpdate();

        Assert.ThrowsAsync<AggregateException>(async () => await ImportInternalAsync(importInput));

        VerifyApiCalls(new List<ModuleApiCallType>()
        {
            ModuleApiCallType.List,
            ModuleApiCallType.Delete,
            ModuleApiCallType.CreateOrUpdate
        });
    }

    async Task ImportInternalAsync(ImportInput importInput)
    {
        await m_CloudCodeModulesImporter!.ImportAsync(importInput, CancellationToken.None);
    }

    void SetupCreateOrUpdate(bool throws = false)
    {
        var setup = m_MockCloudCodeService.Setup(
            cs => cs.UpdateModuleAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()));
        if (throws)
        {
            setup.Throws(new Exception("mock exception"));
            return;
        }

        setup.Returns(Task.FromResult(new ApiResponse<object>(HttpStatusCode.NoContent, new object())));
    }

    void SetupGet(Module module, bool throws = false)
    {
        var setup = m_MockCloudCodeService.Setup(cs => cs.GetModuleAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()));

        if (throws)
        {
            setup.Throws(new Exception("mock exception"));
            return;
        }

        var getModuleResponse = new GetModuleResponse(module.Name.ToString(),
            Language.JS);

        setup.ReturnsAsync(getModuleResponse);
    }

    void SetupList(IEnumerable<ListModulesResponseResultsInner> result, List<Module> modules)
    {
        m_MockCloudCodeService.Setup(
                cs => cs.ListModulesAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(result));

        /* todo
        m_MockCloudCodeModulesDownloader.Setup(
                cs => cs.DownloadModulesForEnvironment(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    result.ToList(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(modules);*/
    }

    void SetupDelete(bool throws = false)
    {
        var setup = m_MockCloudCodeService.Setup(
            cs => cs.DeleteModuleAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()));

        if (throws)
        {
            setup.Throws(new Exception("mock exception"));
            return;
        }

        setup.Returns(Task.FromResult(new ApiResponse<object>(HttpStatusCode.NoContent, new object())));
    }

    void VerifyApiCalls(List<ModuleApiCallType> apiCallTypes)
    {
        var getTimes = apiCallTypes.Contains(ModuleApiCallType.Get) ? Times.Once() : Times.Never();
        m_MockCloudCodeService.Verify(
            cs => cs.GetModuleAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            getTimes);

        var listTimes = apiCallTypes.Contains(ModuleApiCallType.List) ? Times.Once() : Times.Never();
        m_MockCloudCodeService.Verify(
            cs => cs.ListModulesAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            listTimes);

        var createOrUpdateTimes = apiCallTypes.Contains(ModuleApiCallType.CreateOrUpdate) ? Times.Once() : Times.Never();
        m_MockCloudCodeService.Verify(
            cs => cs.UpdateModuleAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            createOrUpdateTimes);

        var deleteTimes = apiCallTypes.Contains(ModuleApiCallType.Delete) ? Times.Once() : Times.Never();
        m_MockCloudCodeService.Verify(
            cs => cs.DeleteModuleAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            deleteTimes);
    }
}
