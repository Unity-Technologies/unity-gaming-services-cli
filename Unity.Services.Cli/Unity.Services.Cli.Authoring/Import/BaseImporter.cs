using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Import.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.Authoring.Import;
/// <summary>
/// Class to import data to an UGS environment from a local file.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class BaseImporter<T> : IImporter
{
    readonly IUnityEnvironment m_UnityEnvironment;
    readonly IZipArchiver<T> m_ZipArchiver;
    protected readonly ILogger Logger;

    /// <summary>
    /// Path where the data to be imported exists
    /// </summary>
    protected abstract string ArchivePath { get; }

    /// <summary>
    /// A path relative to the root of the archive, indicating the name of the entry to be imported
    /// </summary>
    protected abstract string EntryName { get; }

    /// <summary>
    /// Archive extension.
    /// </summary>
    protected abstract string Extension { get; }

    protected ImportInput ImportInput { get; set; } = null!;

    protected BaseImporter(
        IZipArchiver<T> zipArchiver,
        IUnityEnvironment unityEnvironment,
        ILogger logger)
    {
        m_ZipArchiver = zipArchiver;
        m_UnityEnvironment = unityEnvironment;
        Logger = logger;
    }

    public async Task ImportAsync(ImportInput input, CancellationToken cancellationToken)
    {
        ImportInput = input;
        var environmentId = await m_UnityEnvironment.FetchIdentifierAsync();
        var projectId = ImportInput!.CloudProjectId!;
        var archivePath = ArchivePath;
        var extension = Extension;

        if (!string.IsNullOrEmpty(ImportInput.FileName))
        {
            var fileName = Path.GetFileNameWithoutExtension(ImportInput.FileName);
            var fileExtension = Path.GetExtension(ImportInput.FileName);
            extension = string.IsNullOrEmpty(fileExtension) ? Extension : fileExtension;

            archivePath = Path.Join(Path.GetDirectoryName(ArchivePath), fileName);
        }

        var configs = m_ZipArchiver.Unzip(archivePath, EntryName, extension);

        if (ImportInput.DryRun)
        {
            var dryRunResult = OnDryRun(configs);
            Logger.LogResultValue(dryRunResult);
            return;
        }

        var configsOnRemote = await ListConfigsAsync(projectId, environmentId, cancellationToken);

        if (ImportInput.Reconcile)
        {
            await DeleteNonMatchingConfigsAsync(
                projectId,
                environmentId,
                configs,
                configsOnRemote,
                cancellationToken
            );
        }

        await CreateOrUpdateConfigsAsync(projectId, environmentId, configs,  configsOnRemote, cancellationToken);
    }

    /// <summary>
    /// Method to delete config async
    /// </summary>
    /// <param name="projectId">Project Id</param>
    /// <param name="environmentId">Environment Id</param>
    /// <param name="configToDelete">Config to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    protected abstract Task DeleteConfigAsync(
        string projectId,
        string environmentId,
        T configToDelete,
        CancellationToken cancellationToken);

    /// <summary>
    /// Method to get list config async
    /// </summary>
    /// <param name="projectId">Project Id</param>
    /// <param name="environmentId">Environment Id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    protected abstract Task<IEnumerable<T>> ListConfigsAsync(
        string projectId,
        string environmentId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Method to check is config present
    /// </summary>
    /// <param name="config"></param>
    /// <param name="configsOnRemote"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected abstract bool IsConfigPresent(
        T config,
        IEnumerable<T> configsOnRemote,
        CancellationToken cancellationToken);

    /// <summary>
    /// Method to create config async
    /// </summary>
    /// <param name="projectId">Project Id</param>
    /// <param name="environmentId">Environment Id</param>
    /// <param name="config">Config to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Return result of creating config</returns>
    protected abstract Task<bool> TryCreateConfigAsync(
        string projectId,
        string environmentId,
        T config,
        CancellationToken cancellationToken);

    /// <summary>
    /// Method to try update config async
    /// </summary>
    /// <param name="projectId">Project Id</param>
    /// <param name="environmentId">Environment Id</param>
    /// <param name="config">Config to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Return result of updating config</returns>
    protected abstract Task<bool> TryUpdateConfigAsync(
        string projectId,
        string environmentId,
        T config,
        CancellationToken cancellationToken);

    /// <summary>
    /// Method to perform dry run
    /// </summary>
    /// <param name="configs">Configs to perform operation</param>
    /// <returns>Result of DryRun operation</returns>
    protected abstract DryRunResult<T> OnDryRun(IEnumerable<T> configs);

    /// <summary>
    /// Method will detect what configs need to be deleted from remote
    /// </summary>
    /// <param name="localConfigs">Local Configs</param>
    /// <param name="remoteConfigs">Remote Configs</param>
    /// <returns>Configs need to be deleted</returns>
    protected abstract IEnumerable<T> GetConfigsToDelete(IEnumerable<T> localConfigs, IEnumerable<T> remoteConfigs);

    /// <summary>
    /// Method to log import information
    /// </summary>
    /// <param name="importResult">Results of import</param>
    protected abstract void LogImportResult(ImportResult<T> importResult);

    async Task DeleteNonMatchingConfigsAsync(
        string projectId,
        string environmentId,
        IEnumerable<T> configs,
        IEnumerable<T> configsOnRemote,
        CancellationToken cancellationToken)
    {
        var deleteTasks = new List<Task>();
        var configsToDelete = GetNonMatchingConfigsAsync(projectId, environmentId, configs, configsOnRemote, cancellationToken);

        foreach (var configToDelete in configsToDelete)
        {
            deleteTasks.Add(DeleteConfigAsync(projectId, environmentId, configToDelete, cancellationToken));
        }

        await Task.WhenAll(deleteTasks);
    }

    async Task CreateOrUpdateConfigsAsync(
        string projectId,
        string environmentId,
        IEnumerable<T> localConfigs,
        IEnumerable<T> configsOnRemote,
        CancellationToken cancellationToken)
    {
        var taskToConfig = new Dictionary<Task<bool>, T>();
        foreach (var config in localConfigs)
        {
            taskToConfig.Add(TryCreateOrUpdateConfigAsync(
                projectId,
                environmentId,
                config,
                configsOnRemote,
                cancellationToken), config);
        }

        await Task.WhenAll(taskToConfig.Keys);

        var failedTasks = taskToConfig.Keys.Where(t => !t.Result);
        var successfulTasks = taskToConfig.Keys.Where(t => t.Result);
        var failed = failedTasks.Select(failedTask => taskToConfig[failedTask]).ToList();
        var imported = successfulTasks.Select(successfulTask => taskToConfig[successfulTask]).ToList();

        LogImportResult(new ImportResult<T>(imported, failed));
    }

    async Task<bool> TryCreateOrUpdateConfigAsync(
        string projectId,
        string environmentId,
        T config,
        IEnumerable<T> configsOnRemote,
        CancellationToken cancellationToken)
    {
        if (IsConfigPresent(config, configsOnRemote, cancellationToken))
        {
            return await TryUpdateConfigAsync(projectId, environmentId, config, cancellationToken);
        }

        return await TryCreateConfigAsync(projectId, environmentId, config, cancellationToken);
    }

    IEnumerable<T> GetNonMatchingConfigsAsync(
        string projectId,
        string environmentId,
        IEnumerable<T> configs,
        IEnumerable<T> configsOnRemote,
        CancellationToken cancellationToken)
    {
        return GetConfigsToDelete(configs, configsOnRemote);
    }
}
