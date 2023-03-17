using System.IO.Compression;
using Newtonsoft.Json;
using Unity.Services.Cli.Common.Exceptions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Unity.Services.Cli.Authoring.Compression;

public class ZipArchiver<T> : IZipArchiver<T>
{
    string m_ArchiveExtension = "";

    public void Zip(string archivePath, string directoryName, string entryName, string archiveExtension, IReadOnlyList<T> data)
    {
        try
        {
            var fileMode = File.Exists(archivePath) ? FileMode.Open : FileMode.Create;
            m_ArchiveExtension = archiveExtension;
            archivePath = m_ArchiveExtension.StartsWith(".")
                ? $"{archivePath}{m_ArchiveExtension}"
                : $"{archivePath}.{m_ArchiveExtension}";

            using var zipToOpen = new FileStream(archivePath, fileMode);
            using var zipArchive = new ZipArchive(zipToOpen, ZipArchiveMode.Update);

            var directory = zipArchive.CreateEntry($"{directoryName}/");
            var entryPath = Path.Join(directory.FullName, $"{entryName}");
            var entry = zipArchive.CreateEntry(entryPath);
            using var writer = new StreamWriter(entry.Open());

            writer.Write(JsonSerializer.Serialize(data));
        }
        catch(Exception e)
        {
            throw new CliException(e.Message, e.InnerException, ExitCode.HandledError);
        }
    }

    public IEnumerable<T> Unzip(string archivePath, string entryName, string archiveExtension)
    {
        try
        {
            archivePath = archiveExtension.StartsWith(".")
                ? $"{archivePath}{archiveExtension}"
                : $"{archivePath}.{archiveExtension}";
            var unzipped = new List<T>();
            using var zipArchive = ZipFile.OpenRead(archivePath);

            foreach (var entry in zipArchive.Entries)
            {
                if (Path.GetFileName(entry.FullName) != entryName)
                {
                    continue;
                }

                using var reader = new StreamReader(entry.Open());
                var data = reader.ReadToEnd();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };
                unzipped.AddRange(JsonConvert.DeserializeObject<IReadOnlyList<T>>(data, settings) ?? Array.Empty<T>());
            }

            return unzipped;
        }
        catch (Exception e)
        {
            throw new CliException(e.Message, e.InnerException, ExitCode.HandledError);
        }
    }
}
