using System.IO.Abstractions;
using System.IO.Compression;
using Unity.Services.CloudCode.Authoring.Editor.Core.IO;
using IFileSystem = Unity.Services.CloudCode.Authoring.Editor.Core.IO.IFileSystem;

namespace Unity.Services.Cli.CloudCode.IO;

class FileSystem : Common.IO.FileSystem, IFileSystem
{
    IFile m_File;
    IPath m_Path;
    IDirectory m_Directory;

    public FileSystem(
        IFile file,
        IPath path,
        IDirectory directory)
    {
        m_File = file;
        m_Path = path;
        m_Directory = directory;
    }

    public Task Copy(string sourceFileName, string destFileName, bool overwrite, CancellationToken token = default(CancellationToken))
    {
        m_File.Copy(sourceFileName, destFileName, overwrite);
        return Task.CompletedTask;
    }

    public IFileStream CreateFile(string path)
    {
        return new CloudCodeFileStream(m_File.Create(path));
    }

    public void CreateZipFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
    {
        ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName);
    }

    public bool FileExists(string path)
    {
        return m_File.Exists(path);
    }

    public bool DirectoryExists(string path)
    {
        return m_Directory.Exists(path);
    }

    public string? GetDirectoryName(string path)
    {
        return m_Path.GetDirectoryName(path);
    }

    public string GetFullPath(string path)
    {
        return m_Path.GetFullPath(path);
    }

    public string GetFileNameWithoutExtension(string path)
    {
        return m_Path.GetFileNameWithoutExtension(path);
    }

    public string Combine(params string[] paths)
    {
        return m_Path.Combine(paths);
    }

    public string Join(string path1, string path2)
    {
        return m_Path.Join(path1, path2);
    }

    public string ChangeExtension(string path, string extension)
    {
        return m_Path.ChangeExtension(path, extension);
    }

    public string[] DirectoryGetFiles(string path, string searchPattern)
    {
        return Directory.GetFiles(path, searchPattern);
    }

    public string[] DirectoryGetFiles(string path, string searchPattern, SearchOption searchOption)
    {
        return Directory.GetFiles(path, searchPattern, searchOption);
    }

    public DirectoryInfo? DirectoryGetParent(string path)
    {
        return Directory.GetParent(path);
    }

    public void FileMove(string sourceFileName, string destFileName)
    {
        File.Move(sourceFileName, destFileName);
    }

    public void MoveDirectory(string sourceDirName, string destDirName)
    {
        Directory.Move(sourceDirName, destDirName);
    }
}
