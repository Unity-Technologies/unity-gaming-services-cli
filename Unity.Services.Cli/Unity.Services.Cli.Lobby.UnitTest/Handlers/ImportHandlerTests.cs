using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Import.Input;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.Cli.Lobby.Handlers;
using Unity.Services.Cli.Lobby.Handlers.Config;
using Unity.Services.Cli.Lobby.Handlers.ImportExport;
using Unity.Services.Cli.RemoteConfig.Types;

namespace Unity.Services.Cli.Lobby.UnitTest.Handlers;

[TestFixture]
class ImportHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<IRemoteConfigService> m_MockRemoteConfigService = new();
    readonly Mock<ILogger> m_MockLogger = new();
    readonly Mock<IZipArchiver> m_MockArchiver = new();
    readonly Mock<ILoadingIndicator> m_MockLoadingIndicator = new();

    LobbyConfig m_Config = null!;
    LobbyConfig m_ConfigV1 = null!;
    LobbyConfig m_ConfigV2 = null!;

    LobbyImporter m_LobbyImporter = null!;

    const string k_V1Schema = ".*resource:/lobby.*";
    const string k_V2Schema = ".*resource:/lobbyv2.*";

    [SetUp]
    public void SetUp()
    {
        m_Config = JsonConvert.DeserializeObject<LobbyConfig>(
            "{ \"Id\": \"mock_id\", \"Config\": { \"mock_key\": \"mock_value\" } }"
        )!;
        m_ConfigV1 = JsonConvert.DeserializeObject<LobbyConfig>(
            "{ \"Id\": \"mock_id\", \"SchemaId\": \"lobby\", \"Config\": { \"mock_key\": \"mock_value\" } }"
        )!;
        m_ConfigV2 = JsonConvert.DeserializeObject<LobbyConfig>(
            "{ \"Id\": \"mock_id\", \"SchemaId\": \"lobbyv2\", \"Config\": { \"mock_key\": \"mock_value\" } }"
        )!;

        m_MockUnityEnvironment.Reset();
        m_MockRemoteConfigService.Reset();
        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(Guid.NewGuid().ToString());

        m_MockLogger.Reset();

        m_LobbyImporter = new LobbyImporter(
            m_MockRemoteConfigService.Object,
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
            m_LobbyImporter,
            m_MockLoadingIndicator.Object,
            CancellationToken.None
        );

        m_MockLoadingIndicator.Verify(li => li.StartLoadingAsync(ImportHandler.LoadingIndicatorMessage,
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

        SetupExistingConfig(m_Config);

        await m_LobbyImporter.ImportAsync(importInput, CancellationToken.None);

        var archivePath = Path.Join(importInput.InputDirectory, LobbyConstants.ZipName);

        m_MockArchiver.Verify(za => za.UnzipAsync<LobbyConfig>(
            archivePath,
            LobbyConstants.EntryName,
            It.IsAny<CancellationToken>()));
    }

    [Test]
    public async Task ImportAsync_UnzipsWithFileName()
    {
        var fileName = "other.lozip";

        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory",
            FileName = fileName,
            DryRun = true
        };

        SetupExistingConfig(m_Config);

        await m_LobbyImporter.ImportAsync(importInput, CancellationToken.None);

        var archivePath = Path.Join(importInput.InputDirectory, fileName);

        m_MockArchiver.Verify(za => za.UnzipAsync<LobbyConfig>(
            archivePath,
            LobbyConstants.EntryName,
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

        var exception = Assert.ThrowsAsync<CliException>(async () =>
        {
            await m_LobbyImporter.ImportAsync(importInput, CancellationToken.None);
        });
        Assert.That(exception!.Message, Is.EqualTo("The file-name argument must have the extension '.lozip'."));
    }

    [Test]
    public async Task ImportAsync_DryRunDoesNotImport()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory",
            DryRun = true
        };

        SetupExistingConfig(m_Config);

        await m_LobbyImporter.ImportAsync(importInput, CancellationToken.None);

        m_MockRemoteConfigService.Verify(
            rcs => rcs.UpdateConfigAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<IEnumerable<ConfigValue>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ConfigExists_Updates()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory",
        };

        foreach (var tc in new List<(LobbyConfig config, string regex)>
                 {
                     (m_Config, k_V1Schema),
                     (m_ConfigV1, k_V1Schema),
                     (m_ConfigV2, k_V2Schema),
                 })
        {
            SetupExistingConfig(tc.config);

            await m_LobbyImporter.ImportAsync(importInput, CancellationToken.None);

            m_MockRemoteConfigService.Verify(
                rcs => rcs.UpdateConfigAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            m_MockRemoteConfigService.Verify(
                rcs => rcs.ApplySchemaAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsRegex(tc.regex),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            m_MockRemoteConfigService.Verify(
                rcs => rcs.CreateConfigAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<ConfigValue>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }

    [Test]
    public async Task ConfigDoesNotExist_CreatesAndAppliesSchema()
    {
        foreach (var tc in new List<(LobbyConfig config, string regex)>
                 {
                     (m_Config, k_V1Schema),
                     (m_ConfigV1, k_V1Schema),
                     (m_ConfigV2, k_V2Schema),
                 })
        {
            SetupNonExistingConfig(tc.config);

            var importInput = new ImportInput()
            {
                InputDirectory = "mock_input_directory",
            };

            await m_LobbyImporter.ImportAsync(importInput, CancellationToken.None);

            m_MockRemoteConfigService.Verify(
                rcs => rcs.CreateConfigAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<ConfigValue>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            m_MockRemoteConfigService.Verify(
                rcs => rcs.ApplySchemaAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsRegex(tc.regex),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            m_MockRemoteConfigService.Verify(
                rcs => rcs.UpdateConfigAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }


    [Test]
    public async Task Reconcile_Updates()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory",
            Reconcile = true
        };

        SetupExistingConfig(m_Config);

        await m_LobbyImporter.ImportAsync(importInput, CancellationToken.None);

        m_MockRemoteConfigService.Verify(
            rcs => rcs.CreateConfigAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<ConfigValue>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        m_MockRemoteConfigService.Verify(
            rcs => rcs.UpdateConfigAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    void SetupConfigFile(LobbyConfig config)
    {
        m_MockArchiver.Reset();
        m_MockArchiver.Setup(
                za => za.UnzipAsync<LobbyConfig>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IEnumerable<LobbyConfig>>(new List<LobbyConfig> { config }));
    }

    void SetupNonExistingConfig(LobbyConfig config)
    {
        SetupConfigFile(config);
        m_MockRemoteConfigService.Reset();
        m_MockRemoteConfigService.Setup(
                rc => rc.GetAllConfigsFromEnvironmentAsync(It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                return JsonConvert.SerializeObject(new RemoteConfigResponse());
            });

    }
    void SetupExistingConfig(LobbyConfig config)
    {
        SetupConfigFile(config);
        m_MockRemoteConfigService.Reset();
        m_MockRemoteConfigService.Setup(
            rc => rc.GetAllConfigsFromEnvironmentAsync(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                return JsonConvert.SerializeObject(new RemoteConfigResponse
                {
                    Configs = new List<RemoteConfigValue>{
                        new RemoteConfigValue{
                            Id = config.Id,
                            Type = LobbyConstants.ConfigType,
                            Value = new List<LobbyConfigValue>{
                                new LobbyConfigValue{
                                    Key = LobbyConstants.ConfigKey,
                                    SchemaId = config.SchemaId,
                                    Value = config.Config,
                                }
                            },
                        }
                    }
                });
            });
    }
}
