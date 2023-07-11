using System.IO.Abstractions;
using Unity.Services.Multiplay.Authoring.Core.Builds;

namespace Unity.Services.Cli.GameServerHosting.Services;

class FileReaderAdapter : IFileReader
{
    readonly IFile m_File;
    readonly IDirectory m_Directory;

    public FileReaderAdapter(IFile file, IDirectory directory)
    {
        m_File = file;
        m_Directory = directory;
    }

    public IEnumerable<string> EnumerateDirectories(string path) => m_Directory.EnumerateDirectories(path);
    public IEnumerable<string> EnumerateFiles(string path) => m_Directory.EnumerateFiles(path);
    public Stream OpenReadFile(string path) => m_File.OpenRead(path);
    public bool DirectoryExists(string path) => m_Directory.Exists(path);
}
