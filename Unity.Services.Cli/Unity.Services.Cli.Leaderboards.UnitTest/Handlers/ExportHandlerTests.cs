using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Export.Input;
using Unity.Services.Cli.Leaderboards.Handlers.ImportExport;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Service;
using Unity.Services.Cli.Leaderboards.UnitTest.Utils;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.UnitTest.Handlers;

[TestFixture]
class ExportHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ILeaderboardsService> m_MockLeaderboardsService = new();
    readonly Mock<ILogger> m_MockLogger = new();
    readonly Mock<IZipArchiver> m_MockArchiver = new();
    readonly Mock<IFileSystem> m_FileSystemMock = new();
    readonly Mock<ILoadingIndicator> m_MockLoadingIndicator = new();
    readonly IEnumerable<UpdatedLeaderboardConfig> m_Configs = new List<UpdatedLeaderboardConfig>()
    {
        new ("id_1", "name_2")
    };
    LeaderboardExporter? m_LeaderboardExporter;

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockLeaderboardsService.Reset();
        m_MockLogger.Reset();
        m_FileSystemMock.Reset();
        m_LeaderboardExporter = new(m_MockLeaderboardsService.Object,
            m_MockArchiver.Object,
            m_MockUnityEnvironment.Object,
            m_FileSystemMock.Object,
            m_MockLogger.Object);
    }

    [Test]
    public async Task ExportAsync_CallsLoadingIndicator()
    {
        var exportInput = new ExportInput()
        {
            OutputDirectory = "mock_output_directory"
        };

        await ExportHandler.ExportAsync
        (
            null!,
            exportInput,
            m_MockLogger.Object,
            m_LeaderboardExporter,
            m_MockLoadingIndicator.Object,
            CancellationToken.None
        );

        m_MockLoadingIndicator.Verify(li => li.StartLoadingAsync(ExportHandler.k_LoadingIndicatorMessage,
            It.IsAny<Func<StatusContext?, Task>>()));
    }

    [Test]
    public async Task ExportAsync_ExportsAndZips()
    {
        var exportInput = new ExportInput()
        {
            OutputDirectory = "mock_output_directory"
        };
        var input = new ListLeaderboardInput()
        {
            CloudProjectId = TestValues.ValidProjectId,
            Cursor = TestValues.Cursor,
            Limit = TestValues.Limit
        };
        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        m_MockLeaderboardsService.Setup(
                ls => ls.GetLeaderboardsAsync(It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(m_Configs));

        m_FileSystemMock.Setup(
            e => e
                .Directory
                .CreateDirectory(It.IsAny<string>()));
        m_FileSystemMock.Setup(
                e => e
                    .Path
                    .Join(It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Path.Join(exportInput.OutputDirectory, LeaderboardConstants.ZipName));

        m_FileSystemMock.Setup(
                e => e
                    .File
                    .Exists(It.IsAny<string>()))
            .Returns<string>((path) => false);

        m_LeaderboardExporter!.ListLeaderboardInput = input;
        await m_LeaderboardExporter.ExportAsync(exportInput, CancellationToken.None);

        var archivePath = Path.Join(exportInput.OutputDirectory, LeaderboardConstants.ZipName);

        m_MockLeaderboardsService.Verify(
            ls => ls.GetLeaderboardsAsync(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        m_MockArchiver.Verify(za => za.ZipAsync(
            archivePath,
            LeaderboardConstants.EntryName,
            m_Configs,
            It.IsAny<CancellationToken>()));
    }

    [Test]
    public async Task ExportAsync_ExportsAndZipsWithFileName()
    {
        var fileName = "other.lbzip";

        var exportInput = new ExportInput()
        {
            OutputDirectory = "mock_output_directory",
            FileName = fileName
        };
        var input = new ListLeaderboardInput()
        {
            CloudProjectId = TestValues.ValidProjectId,
            Cursor = TestValues.Cursor,
            Limit = TestValues.Limit
        };
        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        m_MockLeaderboardsService.Setup(
                ls => ls.GetLeaderboardsAsync(It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(m_Configs));

        var archivePath = Path.Join(exportInput.OutputDirectory, fileName);

        m_FileSystemMock.Setup(
            e => e
                .Directory
                .CreateDirectory(It.IsAny<string>()));
        m_FileSystemMock.Setup(
                e => e
                    .Path
                    .Join(It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(archivePath);

        m_FileSystemMock.Setup(
                e => e
                    .File
                    .Exists(It.IsAny<string>()))
            .Returns<string>((path) => false);

        m_LeaderboardExporter!.ListLeaderboardInput = input;
        await m_LeaderboardExporter.ExportAsync(exportInput, CancellationToken.None);

        m_MockLeaderboardsService.Verify(
            ls => ls.GetLeaderboardsAsync(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        m_MockArchiver.Verify(za => za.ZipAsync(
            archivePath,
            LeaderboardConstants.EntryName,
            m_Configs,
            It.IsAny<CancellationToken>()));
    }

    [Test]
    public void ExportAsync_FailsWithInvalidExtension()
    {
        var fileName = "other.zip";

        var exportInput = new ExportInput()
        {
            OutputDirectory = "mock_output_directory",
            FileName = fileName
        };

        var input = new ListLeaderboardInput()
        {
            CloudProjectId = TestValues.ValidProjectId,
            Cursor = TestValues.Cursor,
            Limit = TestValues.Limit
        };
        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        m_LeaderboardExporter!.ListLeaderboardInput = input;
        var exception = Assert.ThrowsAsync<CliException>(async () =>
        {
            await m_LeaderboardExporter.ExportAsync(exportInput, CancellationToken.None);
        });

        Assert.That(exception!.Message, Is.EqualTo("The file-name argument must have the extension '.lbzip'."));
    }
}
