using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Export.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.Authoring.Export;

/// <summary>
/// Class to import data to from an UGS environment to a local file.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class BaseExporter<T> : IExporter
{
    readonly IZipArchiver<T> m_ZipArchiver;
    readonly IUnityEnvironment m_UnityEnvironment;
    protected readonly ILogger Logger;

    /// <summary>
    /// Path to the archive to be created
    /// </summary>
    protected abstract string ArchivePath { get; }

    /// <summary>
    /// Archive name.
    /// </summary>
    protected abstract string DirectoryName { get; }

    /// <summary>
    /// A path relative to the root of the archive, indicating the name of the entry to be created.
    /// </summary>
    protected abstract string EntryName { get; }

    /// <summary>
    /// Archive extension.
    /// </summary>
    protected abstract string Extension { get; }

    protected ExportInput ExportInput { get; set; } = null!;

    protected BaseExporter(
        IZipArchiver<T> zipArchiver,
        IUnityEnvironment unityEnvironment,
        ILogger logger)
    {
        m_ZipArchiver = zipArchiver;
        m_UnityEnvironment = unityEnvironment;
        Logger = logger;
    }

    public async Task ExportAsync(ExportInput input, CancellationToken cancellationToken)
    {
        ExportInput = input;
        var projectId = ExportInput.CloudProjectId!;
        var environmentId = await m_UnityEnvironment.FetchIdentifierAsync();
        var configs = (await GetConfigsAsync(projectId, environmentId, cancellationToken)).ToList();
        var archivePath = ArchivePath;
        var extension = Extension;

        if (ExportInput.DryRun)
        {
            var dryRunResult = OnDryRun(configs);
            Logger.LogResultValue(dryRunResult);
            return;
        }

        if (!string.IsNullOrEmpty(ExportInput.FileName))
        {
            var fileName = Path.GetFileNameWithoutExtension(ExportInput.FileName);
            var fileExtension = Path.GetExtension(ExportInput.FileName);
            extension = string.IsNullOrEmpty(fileExtension) ? Extension : fileExtension;

            archivePath = Path.Join(Path.GetDirectoryName(ArchivePath), fileName);
        }

        if (!Directory.Exists(ArchivePath))
        {
            Directory.CreateDirectory(ArchivePath);
        }

        OnBeforeZip(configs);
        m_ZipArchiver.Zip(archivePath, DirectoryName, EntryName, extension, configs);
    }

    protected virtual void OnBeforeZip(IEnumerable<T> configs)
    {
    }

    /// <summary>
    /// Method to get config async
    /// </summary>
    /// <param name="projectId">Project Id</param>
    /// <param name="environmentId">Environment Id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    protected abstract Task<IEnumerable<T>> GetConfigsAsync(
        string projectId,
        string environmentId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Method to perform dry run
    /// </summary>
    /// <param name="configs">Configs to perform operation</param>
    /// <returns>Result of DryRun operation</returns>
    protected abstract DryRunResult<T> OnDryRun(IReadOnlyList<T> configs);
}
