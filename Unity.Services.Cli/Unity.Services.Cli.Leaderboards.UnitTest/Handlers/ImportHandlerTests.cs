using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Import.Input;
using Unity.Services.Cli.Leaderboards.Handlers.ImportExport;
using Unity.Services.Cli.Leaderboards.Service;
using Unity.Services.Cli.Leaderboards.UnitTest.Utils;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Client;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.UnitTest.Handlers;

enum ApiCallType
{
    Create,
    Update,
    Delete,
    Get,
    List
}

[TestFixture]
class ImportHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ILeaderboardsService> m_MockLeaderboardsService = new();
    readonly Mock<ILogger> m_MockLogger = new();
    readonly Mock<IZipArchiver> m_MockArchiver = new();
    readonly Mock<ILoadingIndicator> m_MockLoadingIndicator = new();

    readonly List<UpdatedLeaderboardConfig> m_MockConfigs = new List<UpdatedLeaderboardConfig>()
    {
        new("mock_id_1", "mock_name_1"),
    };

    LeaderboardImporter? m_LeaderboardImporter;


    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockLeaderboardsService.Reset();
        m_MockLogger.Reset();

        m_MockArchiver.Setup(
                za => za.UnzipAsync<UpdatedLeaderboardConfig>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult((IEnumerable<UpdatedLeaderboardConfig>)m_MockConfigs));

        m_LeaderboardImporter = new LeaderboardImporter(
            m_MockLeaderboardsService.Object,
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
            m_MockLogger.Object,
            m_LeaderboardImporter,
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

        var archivePath = Path.Join(importInput.InputDirectory, LeaderboardConstants.ZipName);

        m_MockArchiver.Verify(za => za.UnzipAsync<UpdatedLeaderboardConfig>(
            archivePath,
            LeaderboardConstants.EntryName,
            It.IsAny<CancellationToken>()));
    }

    [Test]
    public async Task ImportAsync_UnzipsWithFileName()
    {
        var fileName = "other.lbzip";

        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory",
            FileName = fileName,
            DryRun = true
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await ImportInternalAsync(importInput);

        var archivePath = Path.Join(importInput.InputDirectory, fileName);

        m_MockArchiver.Verify(za => za.UnzipAsync<UpdatedLeaderboardConfig>(
            archivePath,
            LeaderboardConstants.EntryName,
            It.IsAny<CancellationToken>()));
    }

    [Test]
    public void ImportAsync_FailsWithInvalidExtension()
    {
        var fileName = "other.zip";

        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory",
            FileName = fileName,
            DryRun = true
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        var exception = Assert.ThrowsAsync<CliException>(async () =>
        {
            await ImportInternalAsync(importInput);
        });
        Assert.That(exception!.Message, Is.EqualTo("The file-name argument must have the extension '.lbzip'."));
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
    public async Task ConfigExists_Updates()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory",
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        SetupList(m_MockConfigs);
        SetupUpdate();

        await ImportInternalAsync(importInput);

        VerifyApiCalls(new List<ApiCallType>()
        {
            ApiCallType.List,
            ApiCallType.Update
        });
    }

    [Test]
    public async Task ConfigDoesNotExist_Creates()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory",
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        SetupGet(null!, true);
        SetupCreate();

        await ImportInternalAsync(importInput);

        VerifyApiCalls(new List<ApiCallType>()
        {
            ApiCallType.Create,
            ApiCallType.List
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
        SetupList(new List<UpdatedLeaderboardConfig>()
        {
            new("remote_id",
                "remote_name")
        });
        SetupDelete();
        SetupList(new List<UpdatedLeaderboardConfig>() {new ("to_delete", "to_delete")});
        SetupCreate();

        await ImportInternalAsync(importInput);

        VerifyApiCalls(new List<ApiCallType>()
        {
            ApiCallType.List,
            ApiCallType.Delete,
            ApiCallType.Create
        });
    }

    async Task ImportInternalAsync(ImportInput importInput)
    {
        await m_LeaderboardImporter!.ImportAsync(importInput, CancellationToken.None);
    }

    void SetupCreate()
    {
        m_MockLeaderboardsService.Setup(
                ls => ls.CreateLeaderboardAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ApiResponse<object>(HttpStatusCode.Created, new object())));
    }

    void SetupUpdate()
    {
        m_MockLeaderboardsService.Setup(
                ls => ls.UpdateLeaderboardAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ApiResponse<object>(HttpStatusCode.NoContent, new object())));
    }

    void SetupGet(UpdatedLeaderboardConfig result, bool throws = false)
    {
        var setup = m_MockLeaderboardsService.Setup(lb => lb.GetLeaderboardAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()));

        if (throws)
        {
            setup.Throws(new Exception("mock exception"));
        }
    }

    void SetupList(IEnumerable<UpdatedLeaderboardConfig> result)
    {
        m_MockLeaderboardsService.Setup(
                ls => ls.GetLeaderboardsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(result));
    }

    void SetupDelete()
    {
        m_MockLeaderboardsService.Setup(
                ls => ls.DeleteLeaderboardAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ApiResponse<object>(HttpStatusCode.NoContent, new object())));
    }

    void VerifyApiCalls(List<ApiCallType> apiCallTypes)
    {
        var getTimes = apiCallTypes.Contains(ApiCallType.Get) ? Times.Once() : Times.Never();
        m_MockLeaderboardsService.Verify(
            ls => ls.GetLeaderboardAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            getTimes);

        var listTimes = apiCallTypes.Contains(ApiCallType.List) ? Times.Once() : Times.Never();
        m_MockLeaderboardsService.Verify(
            ls => ls.GetLeaderboardsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()),
            listTimes);

        var createTimes = apiCallTypes.Contains(ApiCallType.Create) ? Times.Once() : Times.Never();
        m_MockLeaderboardsService.Verify(
            ls => ls.CreateLeaderboardAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            createTimes);

        var updateTimes = apiCallTypes.Contains(ApiCallType.Update) ? Times.Once() : Times.Never();
        m_MockLeaderboardsService.Verify(
            ls => ls.UpdateLeaderboardAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            updateTimes);

        var deleteTimes = apiCallTypes.Contains(ApiCallType.Delete) ? Times.Once() : Times.Never();
        m_MockLeaderboardsService.Verify(
            ls => ls.DeleteLeaderboardAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            deleteTimes);
    }
}
