using System.IO.Compression;

namespace Unity.Services.Cli.Authoring.Compression;

public class ZipEntryStream : IDisposable
{
    readonly ZipArchive? m_Archive;
    public Stream Stream { get; }

    public ZipEntryStream(Stream stream, ZipArchive? archive = null)
    {
        m_Archive = archive;
        Stream = stream;
    }

    public void Dispose()
    {
        m_Archive?.Dispose();
    }
}
