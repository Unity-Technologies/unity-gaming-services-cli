using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Import.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Utils;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.ModuleTemplate.Authoring.Core.Batching;

namespace Unity.Services.Cli.Authoring.Import;
/// <summary>
/// Class to import data to an UGS environment from a local file.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class BaseImporter<T> : IImporter
{
    readonly IUnityEnvironment m_UnityEnvironment;
    protected readonly IZipArchiver m_ZipArchiver;
    protected readonly ILogger m_Logger;

    /// <summary>
    /// Path where the data to be imported exists
    /// </summary>
    protected abstract string FileName { get; }

    /// <summary>
    /// A path relative to the root of the archive, indicating the name of the entry to be imported
    /// </summary>
    protected abstract string EntryName { get; }

    protected string? m_ArchivePath;

    protected BaseImporter(
        IZipArchiver zipArchiver,
        IUnityEnvironment unityEnvironment,
        ILogger logger)
    {
        m_ZipArchiver = zipArchiver;
        m_UnityEnvironment = unityEnvironment;
        m_Logger = logger;
    }

    /// <summary>
    /// Import configs in parallel tasks
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="maxParallelTaskLimit">Limit max parallel tasks to reduce 429 too many requests issue, various among services, work together with retry-after header handling, ex RetryAfterSleepDuration in LeaderboardsModule.cs</param>
    /// <exception cref="CliException"></exception>
    public async Task ImportAsync(ImportInput input, CancellationToken cancellationToken, int maxParallelTaskLimit = 10)
    {
        var environmentId = await m_UnityEnvironment.FetchIdentifierAsync();
        var projectId = input!.CloudProjectId!;
        var fileName = ImportExportUtils.ResolveFileName(input.FileName, FileName);

        m_ArchivePath = Path.Join(input.InputDirectory, fileName);
        var localConfigs = await m_ZipArchiver.UnzipAsync<T>(m_ArchivePath, EntryName, cancellationToken);
        var remoteConfigs = await ListConfigsAsync(projectId, environmentId, cancellationToken);

        var state = CreateState(localConfigs, remoteConfigs);

        if (!input.DryRun)
        {
            await ImportConfigsAsync(
                projectId,
                environmentId,
                input.Reconcile,
                state,
                maxParallelTaskLimit,
                cancellationToken);
        }

        var items = state.CreatedItems().Concat(state.UpdatedItems());
        if (input.Reconcile)
        {
            items = items.Concat(state.DeletedItems());
        }

        m_Logger.LogResultValue(new ImportExportResult(items.ToList())
        {
            Header = input.DryRun ? "The following items will be imported:" : "The following items were imported:",
            DryRun = input.DryRun
        });

        var importExceptions = state.ImportExceptions();
        if (importExceptions != null)
        {
            throw importExceptions;
        }
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
    /// Method to create config async
    /// </summary>
    /// <param name="projectId">Project Id</param>
    /// <param name="environmentId">Environment Id</param>
    /// <param name="config">Config to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected abstract Task CreateConfigAsync(
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
    protected abstract Task UpdateConfigAsync(
        string projectId,
        string environmentId,
        T config,
        CancellationToken cancellationToken);

    /// <summary>
    /// Wrap value as ImportExportEntry
    /// </summary>
    /// <param name="value">value to wrap</param>
    /// <returns></returns>
    protected abstract ImportExportEntry<T> ToImportExportEntry(T value);

    protected virtual async Task ImportConfigsAsync(
        string projectId,
        string environmentId,
        bool reconcile,
        ImportState<T> state,
        int maxParallelTaskLimit,
        CancellationToken cancellationToken)
    {
        var createEntryDelegates = CreateDelegatesFromImportExportEntries(
            projectId, environmentId, state.ToCreate, CreateConfigAsync, cancellationToken);

        var updateEntryDelegates = CreateDelegatesFromImportExportEntries(
            projectId, environmentId, state.ToUpdate, UpdateConfigAsync, cancellationToken);

        List<Func<Task>> deleteEntryDelegates = new List<Func<Task>>();

        if (reconcile)
        {
            deleteEntryDelegates = CreateDelegatesFromImportExportEntries(
                projectId, environmentId, state.ToDelete, DeleteConfigAsync, cancellationToken);
        }

        var delegates = createEntryDelegates
            .Concat(updateEntryDelegates)
            .Concat(deleteEntryDelegates);

        await Batching.ExecuteInBatchesAsync(
            delegates,
            cancellationToken,
            maxParallelTaskLimit);
    }

    static List<Func<Task>> CreateDelegatesFromImportExportEntries(
        string projectId,
        string environmentId,
        IReadOnlyCollection<ImportExportEntry<T>> entries,
        Func<string, string, T, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        List<Func<Task>> delegates = new List<Func<Task>>();

        foreach (var entry in entries)
        {
            delegates.Add(async () =>
            {
                try
                {
                    await action(
                        projectId,
                        environmentId,
                        entry.Value,
                        cancellationToken);
                }
                catch (Exception e)
                {
                    entry.Fail(e);
                }
            });
        }

        return delegates;
    }

    protected virtual ImportState<T> CreateState(IEnumerable<T> localConfigs, IEnumerable<T> remoteConfigs)
    {
        var localEntries = localConfigs.Select(ToImportExportEntry).ToList();
        var remoteEntries = remoteConfigs.Select(ToImportExportEntry).ToList();

        var toCreate = localEntries.Except(remoteEntries).ToList();
        var toUpdate = localEntries.Intersect(remoteEntries).ToList();
        var toDelete = remoteEntries.Except(localEntries).ToList();

        return new ImportState<T>(toCreate, toUpdate, toDelete);
    }
}
