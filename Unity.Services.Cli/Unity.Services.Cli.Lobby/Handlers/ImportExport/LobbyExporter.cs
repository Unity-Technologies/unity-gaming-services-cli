using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Export;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.RemoteConfig.Service;

namespace Unity.Services.Cli.Lobby.Handlers.ImportExport;

class LobbyExporter : BaseExporter<LobbyConfig>
{
    readonly IRemoteConfigService m_RemoteConfigService;

    public LobbyExporter(
        IRemoteConfigService remoteConfigService,
        IZipArchiver zipArchiver,
        IUnityEnvironment unityEnvironment,
        IFileSystem fileSystem,
        ILogger logger)
        : base(
        zipArchiver,
        unityEnvironment,
        fileSystem,
        logger)
    {
        m_RemoteConfigService = remoteConfigService;
    }

    protected override string FileName => LobbyConstants.ZipName;
    protected override string EntryName => LobbyConstants.EntryName;
    protected override async Task<IEnumerable<LobbyConfig>> ListConfigsAsync(string projectId, string environmentId, CancellationToken cancellationToken)
    {
        string response = await m_RemoteConfigService.GetAllConfigsFromEnvironmentAsync(
            projectId,
            environmentId,
            LobbyConstants.ConfigType,
            cancellationToken);

        LobbyConfig.TryParse(response, out LobbyConfig? config);

        if (config == null)
        {
            return new List<LobbyConfig>();
        }

        return new List<LobbyConfig>{ config };
    }

    protected override ImportExportEntry<LobbyConfig> ToImportExportEntry(LobbyConfig value)
    {
        return new ImportExportEntry<LobbyConfig>(value.Id.GetHashCode(), LobbyConstants.ConfigDisplayName, value);
    }
}
