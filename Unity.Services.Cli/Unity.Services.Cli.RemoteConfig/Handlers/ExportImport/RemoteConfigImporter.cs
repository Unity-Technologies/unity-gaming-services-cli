using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Import;
using Unity.Services.Cli.Authoring.Import.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Service;

namespace Unity.Services.Cli.RemoteConfig.Handlers.ExportImport;

class RemoteConfigImporter : BaseImporter<RemoteConfigEntryDTO>
{
    readonly ICliRemoteConfigClient m_RemoteConfigClient;
    GetConfigsResult m_GetResult;

    public RemoteConfigImporter(
        ICliRemoteConfigClient remoteConfigClient,
        IZipArchiver zipArchiver,
        IUnityEnvironment unityEnvironment,
        ILogger logger)
        : base(
        zipArchiver,
        unityEnvironment,
        logger)
        {
            m_RemoteConfigClient = remoteConfigClient;
        }

    protected override string FileName => RemoteConfigConstants.ZipName;
    protected override string EntryName => RemoteConfigConstants.EntryName;


    /// Method to get list config async
    protected override async Task<IEnumerable<RemoteConfigEntryDTO>> ListConfigsAsync(
        string cloudProjectId,
        string environmentId,
        CancellationToken cancellationToken)
    {
        m_RemoteConfigClient.Initialize(cloudProjectId, environmentId, cancellationToken);
        m_GetResult = await m_RemoteConfigClient.GetAsync();

        if (!m_GetResult.ConfigsExists)
            return Array.Empty<RemoteConfigEntryDTO>();

        var configsOnRemote = ToDto(m_GetResult.Configs);
        return configsOnRemote;
    }

    /// Method to create config async
    protected override Task CreateConfigAsync(
        string projectId,
        string environmentId,
        RemoteConfigEntryDTO config,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("RC does not have granularity, so it does not apply");
    }

    /// Method to try update config async
    protected override Task UpdateConfigAsync(
        string projectId,
        string environmentId,
        RemoteConfigEntryDTO config,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("RC does not have granularity, so it does not apply");
    }

    /// Method to delete config async
    protected override Task DeleteConfigAsync(
        string projectId,
        string environmentId,
        RemoteConfigEntryDTO configToDelete,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("RC does not have granularity, so it does not apply");
    }

    protected override ImportExportEntry<RemoteConfigEntryDTO> ToImportExportEntry(RemoteConfigEntryDTO value)
    {
        return new ImportExportEntry<RemoteConfigEntryDTO>(value.key.GetHashCode(), value.key, value);
    }

    protected override async Task ImportConfigsAsync(string projectId, string environmentId, bool reconcile, ImportState<RemoteConfigEntryDTO> state, int maxParallelTaskLimit, CancellationToken cancellationToken)
    {
        List<RemoteConfigEntryDTO> configs = new ();
        configs.AddRange(state.ToCreate.Select(c => c.Value));
        configs.AddRange(state.ToUpdate.Select(c => c.Value));
        if (!reconcile)
        {
            // Not adding Deleted values will delete them IE reconcile
            // We add them if reconcile is disabled to keep them
            configs.AddRange(state.ToDelete.Select(c => c.Value));
        }

        var configsToSend = RemoteConfigClient.ToRemoteConfigEntry(configs);

        if (m_GetResult.ConfigsExists)
        {
            await m_RemoteConfigClient.UpdateAsync(configsToSend);
        }
        else
        {
            await m_RemoteConfigClient.CreateAsync(configsToSend);
        }
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
