using System.IO.Abstractions;
using Unity.Services.Cli.Authoring.Exceptions;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.Authoring.Service;

class DeployFileService : IDeployFileService
{
    readonly IFile m_File;
    readonly IDirectory m_Directory;
    readonly IPath m_Path;
    public DeployFileService(IFile file, IDirectory directory, IPath path)
    {
        m_File = file;
        m_Directory = directory;
        m_Path = path;
    }

    public virtual IReadOnlyList<string> ListFilesToDeploy(IReadOnlyList<string> paths, string extension, bool ignoreDirectory)
    {
        if (!paths.Any())
        {
            throw new DeployException("Please specify at least one path to deploy.");
        }

        var files = new List<string>();

        foreach (var path in paths)
        {
            files.AddRange(ListFilesToDeploy(path, extension, ignoreDirectory));
        }
        files = files.Distinct().ToList();
        files.Sort();

        return files;
    }

    protected IReadOnlyList<string> ListFilesToDeploy(string path, string extension, bool ignoreDirectory)
    {
        if (!path.Any())
        {
            throw new DeployException("Please specify at least one path to deploy.");
        }

        var files = new List<string>();

        var fullPath = m_Path.GetFullPath(path);

        if (m_File.Exists(fullPath))
        {
            if (string.Equals(Path.GetExtension(fullPath), extension))
            {
                files.Add(fullPath);
            }
        }
        else if (m_Directory.Exists(fullPath))
        {
            if (!ignoreDirectory)
            {
                try
                {
                    files.AddRange(m_Directory.GetFiles(fullPath, $"*{extension}", SearchOption.AllDirectories));
                }
                catch (UnauthorizedAccessException)
                {
                    throw new CliException($"CLI does not have the permissions to access \"{fullPath}\"", ExitCode.HandledError);
                }
            }
        }
        else
        {
            throw new PathNotFoundException($"\"{fullPath}\"");
        }

        files = files.Distinct().ToList();
        files.Sort();
        return files;
    }
}
