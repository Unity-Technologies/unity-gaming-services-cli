using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Export.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Utils;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.Authoring.Export;

/// <summary>
/// Class to import data to from an UGS environment to a local file.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class BaseExporter<T> : IExporter
{
    readonly IZipArchiver m_ZipArchiver;
    readonly IUnityEnvironment m_UnityEnvironment;
    readonly IFileSystem m_FileSystem;
    readonly ILogger m_Logger;

    /// <summary>
    /// Name of the file to export to.
    /// </summary>
    protected abstract string FileName { get; }

    /// <summary>
    /// A path relative to the root of the archive, indicating the name of the entry to be created.
    /// </summary>
    protected abstract string EntryName { get; }

    protected BaseExporter(
        IZipArchiver zipArchiver,
        IUnityEnvironment unityEnvironment,
        IFileSystem fileSystem,
        ILogger logger)
    {
        m_ZipArchiver = zipArchiver;
        m_UnityEnvironment = unityEnvironment;
        m_FileSystem = fileSystem;
        m_Logger = logger;
    }

    public async Task ExportAsync(ExportInput input, CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var environmentId = await m_UnityEnvironment.FetchIdentifierAsync();
        var fileName = ImportExportUtils.ResolveFileName(input.FileName, FileName);

        var configs = await ListConfigsAsync(projectId, environmentId, cancellationToken);
        var state = new ExportState<T>(configs.Select(ToImportExportEntry).ToList());

        if (!input.DryRun)
        {
            await ExportToZipAsync(input.OutputDirectory, fileName, state, cancellationToken);
        }

        m_Logger.LogResultValue(new ImportExportResult( state.ExportedItems().ToList())
        {
            Header = input.DryRun ? "The following items will be exported:" : "The following items were exported:",
            DryRun = input.DryRun
        });
    }

    async Task ExportToZipAsync(string? outputDirectory, string fileName, ExportState<T> state, CancellationToken cancellationToken)
    {
        if (outputDirectory != null)
        {
            m_FileSystem.Directory.CreateDirectory(outputDirectory);
        }

        var archivePath = Path.Join(outputDirectory, fileName);

        if (m_FileSystem.File.Exists(archivePath))
        {
            throw new CliException($"The filename to export to already exists. Please create a new file", null, ExitCode.HandledError);
        }

        await m_ZipArchiver.ZipAsync(archivePath, EntryName, state.ToExport.Select(c => c.Value), cancellationToken);
        await AfterZip(state.ToExport.Select(c => c.Value), archivePath, cancellationToken);
    }

    protected virtual Task AfterZip(IEnumerable<T> configs, string archivePath, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Method to get config async
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
    /// Wrap value as ImportExportEntry
    /// </summary>
    /// <param name="value">value to wrap</param>
    /// <returns></returns>
    protected abstract ImportExportEntry<T> ToImportExportEntry(T value);
}
