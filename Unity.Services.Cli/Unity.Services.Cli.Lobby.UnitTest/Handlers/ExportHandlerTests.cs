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
using Unity.Services.Cli.Lobby.Handlers.ImportExport;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.Cli.Lobby.Handlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Unity.Services.Cli.Lobby.UnitTest.Handlers;

[TestFixture]
class ExportHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<IRemoteConfigService> m_MockRemoteConfigService = new();
    readonly Mock<ILogger> m_MockLogger = new();
    readonly Mock<IZipArchiver> m_MockArchiver = new();
    readonly Mock<IFileSystem> m_MockFileSystem = new();
    readonly Mock<ILoadingIndicator> m_MockLoadingIndicator = new();

    LobbyConfig m_Config = new()
    {
        Id = "mock_id",
        Config = JsonConvert.DeserializeObject<JObject>("{ \"mock_key\": \"mock_value\" }")
    };

    LobbyExporter m_LobbyExporter = null!;

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(Guid.NewGuid().ToString());

        m_MockRemoteConfigService.Reset();

        var configJson = JsonConvert.SerializeObject(m_Config);
        var configElement = JsonConvert.DeserializeObject<JObject>(configJson);

        m_MockRemoteConfigService.Setup(
            rc => rc.GetAllConfigsFromEnvironmentAsync(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                return JsonConvert.SerializeObject(new RemoteConfigResponse
                {
                    Configs = new List<RemoteConfigResponse.Config>{
                        new RemoteConfigResponse.Config{
                            Id = m_Config.Id,
                            Type = LobbyConstants.ConfigType,
                            Value = new List<RemoteConfigResponse.ConfigValue>{
                                new RemoteConfigResponse.ConfigValue{
                                    Key = LobbyConstants.ConfigKey,
                                    Value = configElement,
                                }
                            },
                        }
                    }
                });
            });

        m_MockArchiver.Reset();
        m_MockLogger.Reset();
        m_MockFileSystem.Reset();
        m_MockFileSystem.Setup(
            e => e
                .Directory
                .CreateDirectory(It.IsAny<string>()));
        m_MockFileSystem.Setup(
                e => e
                    .File
                    .Exists(It.IsAny<string>()))
            .Returns<string>((path) => false);

        m_LobbyExporter = new(m_MockRemoteConfigService.Object,
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

        await ExportHandler.ExportAsync
        (
            exportInput,
            m_LobbyExporter,
            m_MockLoadingIndicator.Object,
            CancellationToken.None
        );

        m_MockLoadingIndicator.Verify(li => li.StartLoadingAsync(ExportHandler.LoadingIndicatorMessage,
            It.IsAny<Func<StatusContext?, Task>>()));
    }

    [Test]
    public async Task ExportAsync_ExportsAndZips()
    {
        var exportInput = new ExportInput()
        {
            OutputDirectory = "mock_output_directory"
        };

        await m_LobbyExporter.ExportAsync(exportInput, CancellationToken.None);

        var archivePath = Path.Join(exportInput.OutputDirectory, LobbyConstants.ZipName);

        m_MockRemoteConfigService.Verify(
            rc => rc.GetAllConfigsFromEnvironmentAsync(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        m_MockArchiver.Verify(za => za.ZipAsync(
            archivePath,
            LobbyConstants.EntryName,
            It.IsAny<IEnumerable<LobbyConfig>>(),
            It.IsAny<CancellationToken>()));
    }

    [Test]
    public async Task ExportAsync_ExportsAndZipsWithFileName()
    {
        var fileName = "other.lozip";

        var exportInput = new ExportInput()
        {
            OutputDirectory = "mock_output_directory",
            FileName = fileName
        };

        await m_LobbyExporter.ExportAsync(exportInput, CancellationToken.None);

        var archivePath = Path.Join(exportInput.OutputDirectory, fileName);

        m_MockRemoteConfigService.Verify(
            rc => rc.GetAllConfigsFromEnvironmentAsync(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        m_MockArchiver.Verify(za => za.ZipAsync(
            archivePath,
            LobbyConstants.EntryName,
            It.IsAny<IEnumerable<LobbyConfig>>(),
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

        var exception = Assert.ThrowsAsync<CliException>(async () =>
        {
            await m_LobbyExporter.ExportAsync(exportInput, CancellationToken.None);
        });

        Assert.That(exception!.Message, Is.EqualTo("The file-name argument must have the extension '.lozip'."));
    }
}
