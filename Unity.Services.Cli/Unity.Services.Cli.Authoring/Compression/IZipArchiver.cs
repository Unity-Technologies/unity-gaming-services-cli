namespace Unity.Services.Cli.Authoring.Compression;

/// <summary>
/// An interface exposed to handlers to make zipping and unzipping archives simpler.
/// </summary>
public interface IZipArchiver
{
    /// <summary>
    /// Create or update a zip file with an entry that contains a collection of data.
    /// </summary>
    /// <typeparam name="T">The type of the data to archive.</typeparam>
    /// <param name="archivePath">The archive to create or update.</param>
    /// <param name="entryName">The path of the file in the archive to update.</param>
    /// <param name="data">A collection of data to archive.</param>
    Task ZipAsync<T>(string archivePath, string entryName, IEnumerable<T> data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract a collection of data from a zip file entry.
    /// </summary>
    /// <typeparam name="T">The type of the data to extract.</typeparam>
    /// <param name="archivePath">The archive to read from.</param>
    /// <param name="entryName">The path of the file in the archive to read.</param>
    /// <returns>A collection of data objects contained in the entry or an empty list if the entry doesn't exist.</returns>
    Task<IEnumerable<T>> UnzipAsync<T>(string archivePath, string entryName, CancellationToken cancellationToken = default);

    ZipEntryStream? GetEntry(string path, string entryName);
}
