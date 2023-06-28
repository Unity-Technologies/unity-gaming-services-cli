using System.IO.Compression;
using Newtonsoft.Json;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.Authoring.Compression;

/// <inheritdoc />
public class ZipArchiver : IZipArchiver
{
    /// <inheritdoc />
    public async Task ZipAsync<T>(string archivePath, string entryName, IEnumerable<T> data, CancellationToken cancellationToken = default)
    {
        using var zipArchive = ZipFile.Open(archivePath, ZipArchiveMode.Create);

        var entry = zipArchive.CreateEntry(entryName);
        using var stream = entry.Open();
        using (StreamWriter writer = new StreamWriter(stream))
            await writer.WriteAsync(JsonConvert.SerializeObject(data));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> UnzipAsync<T>(string archivePath, string entryName, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(archivePath))
        {
            throw new CliException($"The file at '{archivePath}' could not be found. Ensure the file exists and the specified path is correct", null, ExitCode.HandledError);
        }
        using var zipArchive = ZipFile.OpenRead(archivePath);

        var entry = zipArchive.Entries.FirstOrDefault(e => e.FullName == entryName);
        if(entry == null)
        {
            throw new CliException($"The zip '{archivePath}' appears to be malformed.", ExitCode.HandledError);
        }

        using var entryStream = entry.Open();
        using var sr = new StreamReader(entryStream);
        var str = await sr.ReadToEndAsync();
        IEnumerable<T>? data = JsonConvert.DeserializeObject<IEnumerable<T>>(str);
        return data ?? Array.Empty<T>();
    }

    /// <inheritdoc />
    public ZipEntryStream? GetEntry(string path, string entryName)
    {
        var zip = ZipFile.OpenRead(path);
        var entry = zip.GetEntry(entryName);
        if (entry == null)
            return null;
        return new ZipEntryStream(entry.Open(), zip);
    }
}
