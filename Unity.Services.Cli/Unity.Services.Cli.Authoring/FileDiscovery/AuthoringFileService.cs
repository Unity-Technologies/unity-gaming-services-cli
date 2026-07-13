using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Deployment.Core;
using Unity.Services.Deployment.Core.Model;
using Unity.Services.Cli.Authoring.Utils;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Exceptions;

namespace Unity.Services.Cli.Authoring.Service;

class AuthoringFileService : DeploymentDefinitionServiceBase, IAuthoringFileService
{
    public override IReadOnlyList<IDeploymentDefinition> DeploymentDefinitions => m_AllDefinitions.AsReadOnly();

    List<IDeploymentDefinition> m_AllDefinitions;
    readonly IFileDiscoveryService m_FileDiscoveryService;

    public AuthoringFileService(IFileDiscoveryService fileDiscoveryService)
    {
        m_AllDefinitions = new List<IDeploymentDefinition>();
        m_FileDiscoveryService = fileDiscoveryService;
    }

    public IDeploymentDefinitionFilteringResult ResolveAuthoringFiles(
        IReadOnlyList<string> inputPaths,
        IReadOnlyList<string> extensions)
    {
        var inputPathsEnumerated = inputPaths.ToList();
        var extensionsEnumerated = extensions.ToList();

        // 1. Discover all deployment definitions from input paths
        m_AllDefinitions = new List<IDeploymentDefinition>(m_FileDiscoveryService.DiscoverDeploymentDefinitions(inputPaths));

        // 2. Resolve files from each input path, separating ddef inputs from non-ddef inputs
        InitializeFileDictionaries(
            extensionsEnumerated,
            out var ddefInputFiles,
            out var nonDdefInputFiles);

        var filesByDdef = new Dictionary<IDeploymentDefinition, List<AuthoringFile>>();
        var excludedFilesByDdef = new Dictionary<IDeploymentDefinition, List<AuthoringFile>>();

        ResolveInputFilesFromPaths(
            inputPathsEnumerated,
            extensionsEnumerated,
            ddefInputFiles,
            filesByDdef,
            excludedFilesByDdef,
            nonDdefInputFiles);

        // 3. Verify no intersection between non-ddef input files and ddef-resolved files
        var ddefFiles = CreateDeploymentDefinitionFilesResult(
            ddefInputFiles, filesByDdef, excludedFilesByDdef);
        VerifyFileIntersection(nonDdefInputFiles, ddefFiles);

        // 4. Merge all files and build result
        var allFilesByExtension = MergeFilesByExtension(extensionsEnumerated, nonDdefInputFiles, ddefInputFiles);
        var ddefByInputPath = MapInputPathsToDefinitions(inputPathsEnumerated);
        var result = new DeploymentDefinitionFilteringResult(ddefFiles, allFilesByExtension, ddefByInputPath);

        // 5. Clean up and return
        m_AllDefinitions.Clear();
        return result;
    }

    static Dictionary<string, IReadOnlyList<AuthoringFile>> MergeFilesByExtension(
        List<string> extensionsEnumerated,
        Dictionary<string, IReadOnlyList<AuthoringFile>> nonDdefInputFiles,
        Dictionary<string, IReadOnlyList<AuthoringFile>> ddefInputFiles)
    {
        var allFilesByExtension = new Dictionary<string, IReadOnlyList<AuthoringFile>>();
        foreach (var extension in extensionsEnumerated)
        {
            var allFiles = new List<AuthoringFile>(nonDdefInputFiles[extension]);
            allFiles.AddRange(ddefInputFiles[extension]);
            allFilesByExtension.Add(extension, allFiles.Distinct().ToList());
        }

        return allFilesByExtension;
    }

    void ResolveInputFilesFromPaths(
        List<string> inputPathsEnumerated,
        List<string> extensionsEnumerated,
        Dictionary<string, IReadOnlyList<AuthoringFile>> ddefInputFiles,
        Dictionary<IDeploymentDefinition, List<AuthoringFile>> filesByDdef,
        Dictionary<IDeploymentDefinition, List<AuthoringFile>> excludedFilesByDdef,
        Dictionary<string, IReadOnlyList<AuthoringFile>> nonDdefInputFiles)
    {
        foreach (var input in inputPathsEnumerated)
        {
            if (IsDdefFile(input))
            {
                ResolveFilesFromDdefInput(
                    input,
                    extensionsEnumerated,
                    ddefInputFiles,
                    filesByDdef,
                    excludedFilesByDdef);
            }
            else
            {
                ResolveFilesFromNonDdefInput(
                    input,
                    extensionsEnumerated,
                    nonDdefInputFiles);
            }
        }
    }

    static void InitializeFileDictionaries(
        IEnumerable<string> extensions,
        out Dictionary<string, IReadOnlyList<AuthoringFile>> ddefInputFiles,
        out Dictionary<string, IReadOnlyList<AuthoringFile>> nonDdefInputFiles)
    {
        ddefInputFiles = new Dictionary<string, IReadOnlyList<AuthoringFile>>();
        nonDdefInputFiles = new Dictionary<string, IReadOnlyList<AuthoringFile>>();

        foreach (var extension in extensions)
        {
            ddefInputFiles[extension] = new List<AuthoringFile>();
            nonDdefInputFiles[extension] = new List<AuthoringFile>();
        }
    }

    static bool IsDdefFile(string path)
    {
        return Path.GetExtension(path).Equals(DDefConstants.Extension, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves deployable files from a .ddef input path.
    /// For each extension, finds files under the ddef's directory, filters by ownership and excludes.
    /// </summary>
    void ResolveFilesFromDdefInput(
        string ddefInputPath,
        IReadOnlyList<string> extensions,
        Dictionary<string, IReadOnlyList<AuthoringFile>> ddefInputFiles,
        Dictionary<IDeploymentDefinition, List<AuthoringFile>> filesByDdef,
        Dictionary<IDeploymentDefinition, List<AuthoringFile>> excludedFilesByDdef)
    {
        var ddef = m_AllDefinitions.FirstOrDefault(d => Path.GetFullPath(d.Path) == Path.GetFullPath(ddefInputPath));
        if (ddef == null)
            throw new DeployException(
                $"Deployment definition '{ddefInputPath}' was provided as input but was not found during discovery.",
                exitCode: Common.Exceptions.ExitCode.UnhandledError);

        foreach (var extension in extensions)
        {
            var ddefFilesForExtension = m_FileDiscoveryService.GetFilesForDeploymentDefinition(ddef, extension);
            var filesForDdef = new List<AuthoringFile>();
            var excludedFiles = new List<AuthoringFile>();

            FilterFilesAndExcludesForDdef(
                ddef,
                ddefFilesForExtension,
                ref filesForDdef,
                ref excludedFiles);

            // Accumulate files by ddef
            if (!filesByDdef.ContainsKey(ddef))
                filesByDdef.Add(ddef, new List<AuthoringFile>());
            filesByDdef[ddef].AddRange(filesForDdef);

            if (!excludedFilesByDdef.ContainsKey(ddef))
                excludedFilesByDdef.Add(ddef, new List<AuthoringFile>());
            excludedFilesByDdef[ddef].AddRange(excludedFiles);

            // Accumulate files by extension
            var existingFiles = (List<AuthoringFile>)ddefInputFiles[extension];
            existingFiles.AddRange(filesForDdef);
        }
    }

    /// <summary>
    /// Resolves deployable files from a non-.ddef input path (directory or file).
    /// Each resolved file gets its parent ddef attached.
    /// </summary>
    void ResolveFilesFromNonDdefInput(
        string inputPath,
        IReadOnlyList<string> extensions,
        Dictionary<string, IReadOnlyList<AuthoringFile>> nonDdefInputFiles)
    {
        foreach (var extension in extensions)
        {
            var filePaths = m_FileDiscoveryService.ListFilesToDeploy(
                new List<string>
                {
                    inputPath
                },
                extension,
                false) ?? new List<string>();
            var authoringFiles = filePaths.Select(CreateAuthoringFile).ToList();

            var existingFiles = (List<AuthoringFile>)nonDdefInputFiles[extension];
            existingFiles.AddRange(authoringFiles);
        }
    }

    AuthoringFile CreateAuthoringFile(string filePath)
    {
        var parentDdef = DefinitionForPath(filePath);
        return new AuthoringFile(filePath, parentDdef);
    }

    static IDeploymentDefinitionFiles CreateDeploymentDefinitionFilesResult(
        Dictionary<string, IReadOnlyList<AuthoringFile>> ddefInputFiles,
        Dictionary<IDeploymentDefinition, List<AuthoringFile>> filesByDdef,
        Dictionary<IDeploymentDefinition, List<AuthoringFile>> excludedFilesByDdef)
    {
        var filesByDdefFinal = filesByDdef.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<AuthoringFile>)kvp.Value);

        var excludedFilesByDdefFinal = excludedFilesByDdef.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<AuthoringFile>)kvp.Value);

        return new DeploymentDefinitionFiles(
            ddefInputFiles,
            filesByDdefFinal,
            excludedFilesByDdefFinal);
    }

    void FilterFilesAndExcludesForDdef(
        IDeploymentDefinition ddef,
        IReadOnlyList<AuthoringFile> ddefFilesForExtension,
        ref List<AuthoringFile> files,
        ref List<AuthoringFile> excludedFiles)
    {
        foreach (var file in ddefFilesForExtension)
        {
            if (DefinitionForPath(file.Path) == ddef)
            {
                if (this.IsPathExcludedByDeploymentDefinition(file.Path, ddef))
                {
                    excludedFiles.Add(file);
                }
                else
                {
                    files.Add(file);
                }
            }
        }
    }

    internal static void VerifyFileIntersection(
        IReadOnlyDictionary<string, IReadOnlyList<AuthoringFile>> inputFiles,
        IDeploymentDefinitionFiles deploymentDefinitionFiles)
    {
        CheckForIntersection(
            inputFiles,
            deploymentDefinitionFiles.FilesByDeploymentDefinition,
            false);

        CheckForIntersection(
            inputFiles,
            deploymentDefinitionFiles.ExcludedFilesByDeploymentDefinition,
            true);
    }

    static void CheckForIntersection(
        IReadOnlyDictionary<string, IReadOnlyList<AuthoringFile>> filesByExtension,
        IReadOnlyDictionary<IDeploymentDefinition, IReadOnlyList<AuthoringFile>> filesByDdef,
        bool isExcludes)
    {
        var fileIntersection = new Dictionary<IDeploymentDefinition, List<string>>();
        foreach (var extensionFiles in filesByExtension.Values)
        {
            foreach (var (ddef, ddefFiles) in filesByDdef)
            {
                if (!ddefFiles.Any()) continue;

                var intersection =
                    extensionFiles.ToPaths()
                        .Intersect(ddefFiles.ToPaths())
                        .ToList();

                if (intersection.Count == 0) continue;

                if (!fileIntersection.TryGetValue(ddef, out var value))
                {
                    value = new List<string>();
                    fileIntersection.Add(ddef, value);
                }

                value.AddRange(intersection);
            }
        }

        if (fileIntersection.Any())
        {
            throw new DeploymentDefinitionFileIntersectionException(fileIntersection, isExcludes);
        }
    }

    Dictionary<string, IDeploymentDefinition?> MapInputPathsToDefinitions(IReadOnlyList<string> paths)
    {
        var ddefs = new Dictionary<string, IDeploymentDefinition?>();
        foreach (var path in paths)
        {
            ddefs[path] = DefinitionForPath(path);
        }

        return ddefs;
    }
}
