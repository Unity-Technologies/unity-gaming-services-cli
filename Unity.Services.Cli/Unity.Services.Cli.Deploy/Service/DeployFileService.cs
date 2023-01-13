using System.IO.Abstractions;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Deploy.Exceptions;

namespace Unity.Services.Cli.Deploy.Service;

class DeployFileService : IDeployFileService
{
    readonly IFile m_File;
    readonly IDirectory m_Directory;

    public DeployFileService(IFile file, IDirectory directory)
    {
        m_File = file;
        m_Directory = directory;
    }

    public IReadOnlyList<string> ListFilesToDeploy(ICollection<string> paths, string extension)
    {
        if (!paths.Any())
        {
            throw new DeployException("Please specify at least one path to deploy.", ExitCode.HandledError);
        }

        var files = new List<string>();

        foreach (var path in paths)
        {
            if (m_File.Exists(path))
            {
                if (string.Equals(Path.GetExtension(path), extension))
                {
                    files.Add(path);
                }
            }
            else if (m_Directory.Exists(path))
            {
                files.AddRange(m_Directory.GetFiles(path, $"*{extension}", SearchOption.AllDirectories));
            }
            else
            {
                throw new PathNotFoundException(path);
            }
        }
        files.Sort();
        return files;
    }
}
