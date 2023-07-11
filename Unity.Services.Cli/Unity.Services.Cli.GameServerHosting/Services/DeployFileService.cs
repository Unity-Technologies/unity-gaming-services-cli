using System.IO.Abstractions;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.GameServerHosting.Exceptions;

namespace Unity.Services.Cli.GameServerHosting.Services;

class DeployFileService : IDeployFileService
{
    readonly IFile m_File;
    readonly IDirectory m_Directory;

    public DeployFileService(IFile file, IDirectory directory)
    {
        m_File = file;
        m_Directory = directory;
    }

    public IEnumerable<string> ListFilesToDeploy(ICollection<string> paths, string extension)
    {
        if (!paths.Any())
        {
            throw new CliException("Please specify at least one path to deploy.", ExitCode.HandledError);
        }

        foreach (var path in paths)
        {
            if (m_File.Exists(path))
            {
                if (Path.GetExtension(path) != extension)
                {
                    throw new InvalidExtensionException(path, extension);
                }
                yield return path;
            }
            else if (m_Directory.Exists(path))
            {
                foreach (var configPath in m_Directory.EnumerateFiles(path, $"*{extension}", SearchOption.AllDirectories))
                {
                    yield return configPath;
                }
            }
            else
            {
                throw new PathNotFoundException(path);
            }
        }
    }

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken) => m_File.ReadAllTextAsync(path, cancellationToken);
}
