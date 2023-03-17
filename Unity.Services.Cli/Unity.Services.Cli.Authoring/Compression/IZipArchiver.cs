namespace Unity.Services.Cli.Authoring.Compression;

public interface IZipArchiver<T>
{
    void Zip(string archivePath, string directoryName, string entryName, string archiveExtension, IReadOnlyList<T> data);
    IEnumerable<T> Unzip(string archivePath, string entryName, string archiveExtension);
}
