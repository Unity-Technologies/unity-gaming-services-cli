using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Export;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.RemoteConfig.Handlers.ExportImport;

class RemoteConfigExporter : BaseExporter<RemoteConfigEntryDTO>
{
    readonly ICliRemoteConfigClient m_RemoteConfigClient;

    public RemoteConfigExporter(
        ICliRemoteConfigClient remoteConfigClient,
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
            m_RemoteConfigClient = remoteConfigClient;
        }

    protected override string FileName => RemoteConfigConstants.ZipName;
    protected override string EntryName => RemoteConfigConstants.EntryName;

    protected override async Task<IEnumerable<RemoteConfigEntryDTO>> ListConfigsAsync(string projectId, string environmentId, CancellationToken cancellationToken)
    {
        m_RemoteConfigClient.Initialize(projectId, environmentId, cancellationToken);
        var result = await m_RemoteConfigClient.GetAsync();
        return ToDto(result.Configs);
    }

    protected override ImportExportEntry<RemoteConfigEntryDTO> ToImportExportEntry(RemoteConfigEntryDTO value)
    {
        return new ImportExportEntry<RemoteConfigEntryDTO>(value.key.GetHashCode(), value.key, value);
    }
    
    static IReadOnlyList<RemoteConfigEntryDTO> ToDto(IReadOnlyList<RemoteConfigEntry> entries)
    {
        return entries.Select(entry => new RemoteConfigEntryDTO()
        {
            key = entry.Key,
            type = entry.GetEntryConfigType().ToString().ToLower(),
            value = entry.Value
        }).ToList();
    }
}
