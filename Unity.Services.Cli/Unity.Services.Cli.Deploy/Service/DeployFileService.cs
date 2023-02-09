using System.IO.Abstractions;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Deploy.Exceptions;

namespace Unity.Services.Cli.Deploy.Service;

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

    public IReadOnlyList<string> ListFilesToDeploy(IReadOnlyList<string> paths, string extension)
    {
        if (!paths.Any())
        {
            throw new DeployException("Please specify at least one path to deploy.");
        }

        var files = new List<string>();

        foreach (var path in paths)
        {
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
                try
                {
                    files.AddRange(m_Directory.GetFiles(fullPath, $"*{extension}", SearchOption.AllDirectories));
                }
                catch (UnauthorizedAccessException)
                {
                    throw new CliException($"CLI does not have the permissions to access \"{fullPath}\"", ExitCode.HandledError);
                }
            }
            else
            {
                throw new PathNotFoundException($"\"{fullPath}\"");
            }
        }
        files = files.Distinct().ToList();
        files.Sort();
        return files;
    }

    public async Task<string> LoadContentAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            return await m_File.ReadAllTextAsync(filePath, cancellationToken);
        }
        catch (FileNotFoundException exception)
        {
            throw new CliException(exception.Message, ExitCode.HandledError);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new CliException(string.Join(" ", exception.Message,
                "Make sure that the CLI has the permissions to access the file and that the " +
                "specified path points to a file and not a directory."), ExitCode.HandledError);
        }
    }
}
