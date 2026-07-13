using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Cli.Authoring.Service;

class FileDiscoveryService : DeployFileService, IFileDiscoveryService
{
    readonly IDeploymentDefinitionFactory m_Factory;
    readonly ILogger m_Logger;

    public FileDiscoveryService(
        IFile file,
        IDirectory directory,
        IPath path,
        ILogger logger,
        IDeploymentDefinitionFactory factory)
        : base(file, directory, path)
    {
        m_Factory = factory;
        m_Logger = logger;
    }

    public IReadOnlyList<IDeploymentDefinition> DiscoverDeploymentDefinitions(IEnumerable<string> inputPaths)
    {
        var allDdefPaths = new HashSet<string>();

        foreach (var inputPath in inputPaths)
        {
            var parentDdefPaths = DiscoverParentDeploymentDefinitions(inputPath);
            var childDdefPaths = DiscoverChildrenDeploymentDefinitions(inputPath);

            foreach (var ddefPath in parentDdefPaths.Concat(childDdefPaths))
            {
                var ddefFullPath = m_Path.GetFullPath(ddefPath);
                allDdefPaths.Add(ddefFullPath);
            }
        }

        return CreateDefinitionsFromPaths(allDdefPaths.Distinct());
    }

    public IReadOnlyList<AuthoringFile> GetFilesForDeploymentDefinition(
        IDeploymentDefinition deploymentDefinition,
        string extension)
    {
        var files = new List<AuthoringFile>();
        var ddefDirectory = m_Path.GetDirectoryName(deploymentDefinition.Path);
        if (ddefDirectory != null)
        {
            var filesForExtension = ListFilesToDeploy(
                ddefDirectory,
                extension,
                false);

            files.AddRange(filesForExtension.Select(f => new AuthoringFile(f, deploymentDefinition)));
        }

        return files;
    }

    /// <summary>
    /// Creates deployment definition objects from discovered paths, validating
    /// that no directory contains multiple .ddef files.
    /// </summary>
    IReadOnlyList<IDeploymentDefinition> CreateDefinitionsFromPaths(IEnumerable<string> ddefPaths)
    {
        var allDdefs = new List<IDeploymentDefinition>();
        var ddefDirectories = new Dictionary<string, IDeploymentDefinition>();

        foreach (var ddefPath in ddefPaths)
        {
            var ddef = m_Factory.CreateDeploymentDefinition(ddefPath);

            var ddefDirectory = m_Path.GetDirectoryName(ddefPath);
            if (ddefDirectory != null)
            {
                if (!ddefDirectories.ContainsKey(ddefDirectory))
                {
                    ddefDirectories.Add(ddefDirectory, ddef);
                }
                else
                {
                    throw new MultipleDeploymentDefinitionInDirectoryException(
                        ddefDirectories[ddefDirectory],
                        ddef,
                        ddefDirectory);
                }
            }

            allDdefs.Add(ddef);
        }

        return allDdefs;
    }

    /// <summary>
    /// Discovers child .ddef files by recursively searching subdirectories.
    /// </summary>
    /// <param name="path">The path to start searching from</param>
    /// <returns>List of discovered ddef file paths</returns>
    /// <exception cref="MultipleDeploymentDefinitionInDirectoryException">
    /// Thrown when multiple .ddef files exist in the same directory
    /// </exception>
    IReadOnlyList<string> DiscoverChildrenDeploymentDefinitions(string path)
    {
        var directoryPath = GetDirectoryPath(path);
        if (directoryPath == null)
            return Array.Empty<string>();

        try
        {
            var allDdefFiles = m_Directory.GetFiles(
                directoryPath,
                $"*{DDefConstants.Extension}",
                SearchOption.AllDirectories
            );

            var discoveredDdefs = new List<string>();
            var ddefsByDirectory = allDdefFiles.GroupBy(f => m_Path.GetDirectoryName(f));

            foreach (var directoryGroup in ddefsByDirectory)
            {
                var ddefsInDirectory = directoryGroup.ToList();
                ValidateNoDuplicateDdefsInDirectory(ddefsInDirectory, directoryGroup.Key);
                discoveredDdefs.AddRange(ddefsInDirectory);
            }

            return discoveredDdefs;
        }
        catch (UnauthorizedAccessException ex)
        {
            m_Logger.LogWarning($"Access denied while searching subdirectories of \"{directoryPath}\": {ex.Message}");
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Discovers parent .ddef files by walking up the directory tree.
    /// Stops at the first .ddef found, at the filesystem root, or on access denied.
    /// </summary>
    /// <param name="path">The path to start searching from</param>
    /// <returns>List of discovered ddef file paths</returns>
    /// <exception cref="MultipleDeploymentDefinitionInDirectoryException">
    /// Thrown when multiple .ddef files exist in the same directory
    /// </exception>
    IEnumerable<string> DiscoverParentDeploymentDefinitions(string path)
    {
        var directoryPath = GetDirectoryPath(path);
        if (directoryPath == null)
            return Enumerable.Empty<string>();

        while (!string.IsNullOrEmpty(directoryPath))
        {
            string[] ddefFiles;
            try
            {
                ddefFiles = m_Directory.GetFiles(
                    directoryPath,
                    $"*{DDefConstants.Extension}",
                    SearchOption.TopDirectoryOnly
                );

                if (ddefFiles.Any())
                {
                    ValidateNoDuplicateDdefsInDirectory(ddefFiles, directoryPath);
                    return new[] { ddefFiles[0] };
                }

            }
            catch (UnauthorizedAccessException)
            {
                m_Logger.LogWarning(
                    $"Access denied to directory \"{directoryPath}\". Unable to continue searching parent directories for deployment definition files. Check directory permissions if a .ddef file should be accessible in this location.");
                break;
            }

            var parentPath = m_Path.GetDirectoryName(directoryPath);
            if (parentPath == directoryPath)
                break;

            directoryPath = parentPath;
        }

        return Enumerable.Empty<string>();
    }

    string? GetDirectoryPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        var directoryPath = m_File.Exists(path)
            ? m_Path.GetDirectoryName(path)
            : path;

        if (string.IsNullOrEmpty(directoryPath))
            return null;

        directoryPath = m_Path.GetFullPath(directoryPath);

        return !string.IsNullOrEmpty(directoryPath) && m_Directory.Exists(directoryPath)
            ? directoryPath
            : null;
    }

    void ValidateNoDuplicateDdefsInDirectory(ICollection<string> ddefFiles, string? directoryPath)
    {
        if (ddefFiles.Count > 1)
        {
            throw new MultipleDeploymentDefinitionInDirectoryException(
                m_Factory.CreateDeploymentDefinition(ddefFiles.ElementAt(0)),
                m_Factory.CreateDeploymentDefinition(ddefFiles.ElementAt(1)),
                directoryPath ?? string.Empty
            );
        }
    }
}
