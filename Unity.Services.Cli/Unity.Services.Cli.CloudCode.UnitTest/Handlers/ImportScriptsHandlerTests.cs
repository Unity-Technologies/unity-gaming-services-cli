using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Import.Input;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Handlers.ImportExport.Scripts;
using Unity.Services.Cli.CloudCode.Model;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.UnitTest.Utils;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using Language = Unity.Services.CloudCode.Authoring.Editor.Core.Model.Language;

namespace Unity.Services.Cli.CloudCode.UnitTest.Handlers;

enum ApiCallType
{
    Create,
    Update,
    Delete,
    Get,
    List,
    Publish
}

[TestFixture]
class ImportScriptsHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ICloudCodeService> m_MockCloudCodeService = new();
    readonly Mock<ICloudCodeScriptParser> m_MockCloudCodeScriptParser = new();
    readonly Mock<ILogger> m_MockLogger = new();
    readonly Mock<IZipArchiver> m_MockArchiver = new();
    readonly Mock<ILoadingIndicator> m_MockLoadingIndicator = new();

    readonly static DateTime DateNow = DateTime.Now;

    readonly IEnumerable<ListScriptsResponseResultsInner> m_ScriptsListSingleScriptResponse =
        new List<ListScriptsResponseResultsInner>()
        {
            new("test1", ScriptType.API, Gateway.CloudCodeApiV1.Generated.Model.Language.JS, true, DateNow, 1),

        };


    readonly CloudCodeScript m_MockNonDuplicateScript =  new (new ScriptName("test"), Language.JS, "", "{}", new List<CloudCodeParameter>(), DateNow.ToString());
    // This mock script updates the existing script in m_MockScripts if used within tests
    readonly CloudCodeScript m_MockScript =  new (new ScriptName("test1"), Language.JS, "", "{}", new List<CloudCodeParameter>(), DateNow.ToString());

    readonly List<CloudCodeScript> m_MockScripts = new()
    {
        new(new ScriptName("test1"), Language.JS, "", "{}", new List<CloudCodeParameter>(), DateNow.ToString()),
        new(new ScriptName("test2"), Language.JS, "","{}", new List<CloudCodeParameter>(), DateNow.ToString()),
        new(new ScriptName("test3"), Language.JS, "","{}", new List<CloudCodeParameter>(), DateNow.ToString()),
        new(new ScriptName("test4"), Language.JS, "","{}", new List<CloudCodeParameter>(), DateNow.ToString()),
    };

    CloudCodeScriptsImporter? m_CloudCodeScriptsImporter;


    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockCloudCodeService.Reset();
        m_MockLogger.Reset();
        m_MockCloudCodeScriptParser.Reset();
        m_MockArchiver.Setup(
                za => za.UnzipAsync<CloudCodeScript>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult((IEnumerable<CloudCodeScript>)m_MockScripts));

        m_CloudCodeScriptsImporter = new CloudCodeScriptsImporter(
            m_MockCloudCodeService.Object,
            m_MockArchiver.Object,
            m_MockUnityEnvironment.Object,
            m_MockLogger.Object);
    }

    [Test]
    public async Task ImportAsync_CallsLoadingIndicator()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory"
        };

        await ImportHandler.ImportAsync(
            importInput,
            m_CloudCodeScriptsImporter,
            m_MockLoadingIndicator.Object,
            CancellationToken.None
        );

        m_MockLoadingIndicator.Verify(li => li.StartLoadingAsync(ImportHandler.k_LoadingIndicatorMessage,
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

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await ImportInternalAsync(importInput);

        var archivePath = Path.Join(importInput.InputDirectory, CloudCodeConstants.JavascriptZipName);

        m_MockArchiver.Verify(za => za.UnzipAsync<CloudCodeScript>(
            archivePath,
            CloudCodeConstants.ScriptsEntryName, CancellationToken.None));
    }

    [Test]
    public async Task ImportAsync_DryRunDoesNotImport()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory",
            DryRun = true
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await ImportInternalAsync(importInput);

        VerifyApiCalls(new List<ApiCallType>
        {
            ApiCallType.List
        });
    }

    [Test]
    public async Task ScriptExists_Updates()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory",
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        SetupList(m_ScriptsListSingleScriptResponse);
        SetupGet(new List<CloudCodeScript>(){m_MockScript});
        SetupUpdate();

        m_MockArchiver.Setup(
                za => za.UnzipAsync<CloudCodeScript>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IEnumerable<CloudCodeScript>>(new List<CloudCodeScript>(){m_MockScript}));
        m_MockCloudCodeScriptParser.Setup(p => p.ParseScriptParametersAsync(
                It.Is<string>(s => s == m_MockNonDuplicateScript.Body),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ParseScriptParametersResult(m_MockNonDuplicateScript.Parameters.Any(),
                new ScriptParameter[0])));
        await ImportInternalAsync(importInput);

        VerifyApiCalls(new List<ApiCallType>()
        {
            ApiCallType.List,
            ApiCallType.Get,
            ApiCallType.Update,
            ApiCallType.Publish
        });
    }

    [Test]
    public async Task ConfigDoesNotExist_Creates()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory",
        };

        m_MockArchiver.Setup(
                za => za.UnzipAsync<CloudCodeScript>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IEnumerable<CloudCodeScript>>(new List<CloudCodeScript>(){m_MockScript}));

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        SetupGet(null!, true);
        SetupCreate();
        m_MockCloudCodeScriptParser.Setup(p =>
            p.ParseScriptParametersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ParseScriptParametersResult(m_MockScript.Parameters.Any(), new ScriptParameter[0])));

        await ImportInternalAsync(importInput);

        VerifyApiCalls(new List<ApiCallType>()
        {
            ApiCallType.Create,
            ApiCallType.List,
            ApiCallType.Publish
        });
    }

    [Test]
    public async Task Reconcile_Deletes()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory",
            Reconcile = true
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        m_MockArchiver.Setup(
                za => za.UnzipAsync<CloudCodeScript>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IEnumerable<CloudCodeScript>>(new List<CloudCodeScript>(){m_MockNonDuplicateScript}));
        m_MockCloudCodeScriptParser.Setup(p => p.ParseScriptParametersAsync(
                It.Is<string>(s => s == m_MockNonDuplicateScript.Body),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ParseScriptParametersResult(m_MockNonDuplicateScript.Parameters.Any(),
                Array.Empty<ScriptParameter>())));


        SetupList(new List<ListScriptsResponseResultsInner>(){new(m_MockNonDuplicateScript.Name.ToString(), ScriptType.API, Gateway.CloudCodeApiV1.Generated.Model.Language.JS, true, DateNow, 1),});
        SetupGet(new List<CloudCodeScript>(){m_MockNonDuplicateScript});

        SetupDelete();

        SetupList(m_ScriptsListSingleScriptResponse);
        SetupGet(new List<CloudCodeScript>(){m_MockScript});

        SetupCreate();

        await ImportInternalAsync(importInput);

        VerifyApiCalls(new List<ApiCallType>()
        {
            ApiCallType.List,
            ApiCallType.Get,
            ApiCallType.Delete,
            ApiCallType.Create,
            ApiCallType.Publish
        });
    }

    [Test]
    public void SucceedsOnRepublish()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory",
            Reconcile = true
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        m_MockArchiver.Setup(
                za => za.UnzipAsync<CloudCodeScript>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IEnumerable<CloudCodeScript>>(new List<CloudCodeScript>(){m_MockNonDuplicateScript}));
        m_MockCloudCodeScriptParser
            .Setup(p => p.ParseScriptParametersAsync(
                It.Is<string>(s => s == m_MockNonDuplicateScript.Body),
                It.IsAny<CancellationToken>()))
            .Returns(
                Task.FromResult(new ParseScriptParametersResult(m_MockNonDuplicateScript.Parameters.Any(),
                Array.Empty<ScriptParameter>())));
        var error = JsonConvert.SerializeObject(
            new {
                code = CloudCodeScriptsImporter.ScriptAlreadyActive
            });

        var exception = new ApiException(
            (int)HttpStatusCode.BadRequest,
            string.Empty,
            error);

        m_MockCloudCodeService
            .Setup(
                c => c.PublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    0,
                    It.IsAny<CancellationToken>()))
            .Throws(exception);

        SetupDelete();
        SetupList(m_ScriptsListSingleScriptResponse);
        SetupGet(new List<CloudCodeScript>(){m_MockScript});
        SetupCreate();

        Assert.DoesNotThrowAsync(async () => await ImportInternalAsync(importInput));
    }

    async Task ImportInternalAsync(ImportInput importInput)
    {
        await m_CloudCodeScriptsImporter!.ImportAsync(importInput, CancellationToken.None);
    }

    void SetupCreate(int creations = 1)
    {

        var setup = m_MockCloudCodeService.SetupSequence(
            cs => cs.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                ScriptType.API,
                Gateway.CloudCodeApiV1.Generated.Model.Language.JS,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<ScriptParameter>>(),
                It.IsAny<CancellationToken>()));

        for (int i = 0; i < creations; i++)
        {
            setup.Returns(Task.FromResult(new ApiResponse<object>(HttpStatusCode.Created, new object())));
        }
    }

    void SetupUpdate()
    {
        m_MockCloudCodeService.Setup(
                cs => cs.UpdateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<ScriptParameter>>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ApiResponse<object>(HttpStatusCode.NoContent, new object())));
    }

    void SetupGet(List<CloudCodeScript> scripts, bool throws = false)
    {
        var setup = m_MockCloudCodeService.SetupSequence(cs => cs.GetAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()));


        if (throws)
        {
            setup.Throws(new Exception("mock exception"));
            return;
        }


        foreach (var script in scripts)
        {
            var getScriptResponse = new GetScriptResponse(script.Name.ToString(),
                ScriptType.API,
                Gateway.CloudCodeApiV1.Generated.Model.Language.JS,
                activeScript: new GetScriptResponseActiveScript(
                    script.Body,
                    1,
                    DateNow,
                    new List<ScriptParameter>()),
                new List<GetScriptResponseVersionsInner>(),
                new List<ScriptParameter>());

            setup.ReturnsAsync(getScriptResponse);
        }

    }

    void SetupList(IEnumerable<ListScriptsResponseResultsInner> result)
    {
        m_MockCloudCodeService.Setup(
                cs => cs.ListAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(result));
    }

    void SetupDelete()
    {
        m_MockCloudCodeService.Setup(
                cs => cs.DeleteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ApiResponse<object>(HttpStatusCode.NoContent, new object())));
    }

    void VerifyApiCalls(List<ApiCallType> apiCallTypes)
    {
        var getTimes = apiCallTypes.Contains(ApiCallType.Get) ? Times.Once() : Times.Never();
        m_MockCloudCodeService.Verify(
            cs => cs.GetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            getTimes);

        var listTimes = apiCallTypes.Contains(ApiCallType.List) ? Times.Once() : Times.Never();
        m_MockCloudCodeService.Verify(
            cs => cs.ListAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            listTimes);

        var createTimes = apiCallTypes.Contains(ApiCallType.Create) ? Times.Once() : Times.Never();
        m_MockCloudCodeService.Verify(
            cs => cs.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                ScriptType.API,
                Gateway.CloudCodeApiV1.Generated.Model.Language.JS,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<ScriptParameter>>(),
                It.IsAny<CancellationToken>()),
            createTimes);

        var updateTimes = apiCallTypes.Contains(ApiCallType.Update) ? Times.Once() : Times.Never();
        m_MockCloudCodeService.Verify(
            cs => cs.UpdateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<ScriptParameter>>(),
                It.IsAny<CancellationToken>()),
            updateTimes);

        var publishTimes = apiCallTypes.Contains(ApiCallType.Publish) ? Times.Once() : Times.Never();
        m_MockCloudCodeService.Verify(
            cs => cs.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            publishTimes);

        var deleteTimes = apiCallTypes.Contains(ApiCallType.Delete) ? Times.Once() : Times.Never();
        m_MockCloudCodeService.Verify(
            cs => cs.DeleteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            deleteTimes);
    }
}
