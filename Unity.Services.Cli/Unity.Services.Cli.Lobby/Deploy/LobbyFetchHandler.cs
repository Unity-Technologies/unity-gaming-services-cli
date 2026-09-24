using System.IO.Abstractions;
using Newtonsoft.Json;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Lobby.Handlers;
using Unity.Services.Cli.Lobby.Handlers.Config;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Lobby.Deploy;

class LobbyFetchHandler
{
    readonly IRemoteConfigService m_RemoteConfigService;
    readonly IFileSystem m_FileSystem;

    public LobbyFetchHandler(
        IRemoteConfigService remoteConfigService,
        IFileSystem fileSystem)
    {
        m_RemoteConfigService = remoteConfigService;
        m_FileSystem = fileSystem;
    }

    public async Task<FetchResult> FetchAsync(
        string projectId,
        string environmentId,
        string? localFilePath,
        string defaultDirectory,
        bool dryRun,
        bool reconcile,
        CancellationToken cancellationToken)
    {
        var updated = new List<IDeploymentItem>();
        var deleted = new List<IDeploymentItem>();
        var created = new List<IDeploymentItem>();
        var fetched = new List<IDeploymentItem>();
        var failed = new List<IDeploymentItem>();

        LobbyConfig? remoteConfig;
        try
        {
            remoteConfig = await GetRemoteConfigAsync(projectId, environmentId, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            var failedItem = new LobbyDeploymentItem(defaultDirectory)
            {
                Status = Statuses.GetFailedToFetch(e.Message)
            };
            failed.Add(failedItem);
            return new FetchResult(updated, deleted, created, fetched, failed, dryRun);
        }

        if (remoteConfig == null)
        {
            if (reconcile && localFilePath != null)
            {
                await ReconcileDeleteLocalAsync(
                    localFilePath, dryRun, deleted, fetched, failed, cancellationToken);
            }

            return new FetchResult(updated, deleted, created, fetched, failed, dryRun);
        }

        bool isCreate = localFilePath == null;
        if (isCreate)
        {
            if (!reconcile)
            {
                return new FetchResult(updated, deleted, created, fetched, failed, dryRun);
            }

            localFilePath = Path.Combine(defaultDirectory, LobbyConstants.FileName);
        }

        var item = new LobbyDeploymentItem(localFilePath!);

        if (!dryRun)
        {
            try
            {
                var dir = Path.GetDirectoryName(localFilePath!);
                if (!string.IsNullOrEmpty(dir))
                {
                    m_FileSystem.Directory.CreateDirectory(dir);
                }

                var fileContent = BuildFetchedJson(remoteConfig);
                await m_FileSystem.File.WriteAllTextAsync(localFilePath!, fileContent, cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                item.Status = Statuses.GetFailedToFetch(e.Message);
                failed.Add(item);
                return new FetchResult(updated, deleted, created, fetched, failed, dryRun);
            }
        }

        item.Progress = 100f;
        var action = isCreate ? Statuses.Created : Statuses.Updated;
        item.Status = new DeploymentStatus(Statuses.Fetched, action, SeverityLevel.Success);

        if (isCreate)
        {
            created.Add(item);
        }
        else
        {
            updated.Add(item);
        }

        fetched.Add(item);
        return new FetchResult(updated, deleted, created, fetched, failed, dryRun);
    }

    async Task<LobbyConfig?> GetRemoteConfigAsync(
        string projectId,
        string environmentId,
        CancellationToken cancellationToken)
    {
        var response = await m_RemoteConfigService.GetAllConfigsFromEnvironmentAsync(
            projectId, environmentId, LobbyConstants.ConfigType, cancellationToken);

        if (LobbyConfig.TryParse(response, out var remoteConfig))
        {
            return remoteConfig;
        }

        return null;
    }

    async Task ReconcileDeleteLocalAsync(
        string filePath,
        bool dryRun,
        List<IDeploymentItem> deleted,
        List<IDeploymentItem> fetched,
        List<IDeploymentItem> failed,
        CancellationToken cancellationToken)
    {
        var deleteItem = new LobbyDeploymentItem(filePath);

        if (!dryRun)
        {
            try
            {
                m_FileSystem.File.Delete(filePath);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                deleteItem.Status = Statuses.GetFailedToFetch(e.Message);
                failed.Add(deleteItem);
                return;
            }
        }

        deleteItem.Progress = 100f;
        deleteItem.Status = new DeploymentStatus(Statuses.Fetched, Statuses.Deleted, SeverityLevel.Success);
        deleted.Add(deleteItem);
        fetched.Add(deleteItem);
    }

    internal static string BuildFetchedJson(LobbyConfig config)
    {
        var fileContent = new LobbyConfigFileContent(
            LobbyConfigFile.k_SchemaUrl,
            string.IsNullOrEmpty(config.SchemaId) ? null : config.SchemaId,
            config.Config);

        return JsonConvert.SerializeObject(fileContent, Formatting.Indented) + "\n";
    }
}
