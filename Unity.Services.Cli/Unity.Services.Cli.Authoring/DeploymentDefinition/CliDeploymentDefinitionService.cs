using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Deployment.Core;
using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Cli.Authoring.Service;

class CliDeploymentDefinitionService : DeploymentDefinitionServiceBase, ICliDeploymentDefinitionService
{
    public const string Extension = ".ddef";
    public override IReadOnlyList<IDeploymentDefinition> DeploymentDefinitions => m_AllDefinitions.AsReadOnly();

    List<IDeploymentDefinition> m_AllDefinitions;
    List<IDeploymentDefinition> m_InputDefinitions;
    readonly IDeploymentDefinitionFileService m_FileService;

    public CliDeploymentDefinitionService(IDeploymentDefinitionFileService fileService)
    {
        m_AllDefinitions = new List<IDeploymentDefinition>();
        m_InputDefinitions = new List<IDeploymentDefinition>();
        m_FileService = fileService;
    }

    public IDeploymentDefinitionFilteringResult GetFilesFromInput(
        IEnumerable<string> inputPaths,
        IEnumerable<string> extensions)
    {
        var inputPathsEnumerated = inputPaths.ToList();
        var extensionsEnumerated = extensions.ToList();

        var ddefFiles = GetDeploymentDefinitionFiles(
            inputPathsEnumerated,
            extensionsEnumerated);

        var inputFilesByExtension = extensionsEnumerated
            .ToDictionary(
                extension => extension,
                extension => m_FileService.ListFilesToDeploy(inputPathsEnumerated, extension, false) ?? new List<string>());

        VerifyFileIntersection(
            inputFilesByExtension,
            ddefFiles);

        var allFilesByExtension = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var extension in extensionsEnumerated)
        {
            var allFiles = new List<string>(inputFilesByExtension[extension]);
            allFiles.AddRange(ddefFiles.FilesByExtension[extension]);
            allFilesByExtension.Add(extension, allFiles.Distinct().ToList());
        }

        m_AllDefinitions.Clear();
        m_InputDefinitions.Clear();

        return new DeploymentDefinitionFilteringResult(ddefFiles, allFilesByExtension);
    }

    internal IDeploymentDefinitionFiles GetDeploymentDefinitionFiles(IEnumerable<string> inputPaths, IEnumerable<string> extensions)
    {
        var inputResult = m_FileService.GetDeploymentDefinitionsForInput(inputPaths);
        m_AllDefinitions = new List<IDeploymentDefinition>(inputResult.AllDeploymentDefinitions);
        m_InputDefinitions = new List<IDeploymentDefinition>(inputResult.InputDeploymentDefinitions);

        var filesByExtension = new Dictionary<string, IReadOnlyList<string>>();
        var excludedFilesByDdef = new Dictionary<IDeploymentDefinition, List<string>>();
        var filesByDdef = new Dictionary<IDeploymentDefinition, List<string>>();
        foreach (var extension in extensions)
        {
            var filesForExtension = new List<string>();
            foreach (var ddef in m_InputDefinitions)
            {
                var filesForDdef = new List<string>();
                var ddefFilesForExtension = m_FileService.GetFilesForDeploymentDefinition(ddef, extension);
                var excludedFiles = new List<string>();
                FilterFilesAndExcludesForDdef(
                    ddef,
                    ddefFilesForExtension,
                    ref filesForDdef,
                    ref excludedFiles);

                if (!filesByDdef.ContainsKey(ddef))
                {
                    filesByDdef.Add(ddef, new List<string>());
                }
                filesByDdef[ddef].AddRange(filesForDdef);

                if (!excludedFilesByDdef.ContainsKey(ddef))
                {
                    excludedFilesByDdef.Add(ddef, new List<string>());
                }
                excludedFilesByDdef[ddef].AddRange(excludedFiles);

                filesForExtension.AddRange(filesForDdef);
            }

            filesByExtension.Add(extension, filesForExtension);
        }

        var filesByDdefFinal = new Dictionary<IDeploymentDefinition, IReadOnlyList<string>>();
        foreach (var (ddef, files) in filesByDdef)
        {
            filesByDdefFinal.Add(ddef, files);
        }

        var excludedFilesByDdefFinal = new Dictionary<IDeploymentDefinition, IReadOnlyList<string>>();
        foreach (var (ddef, excludedFiles) in excludedFilesByDdef)
        {
            excludedFilesByDdefFinal.Add(ddef, excludedFiles);
        }

        return new DeploymentDefinitionFiles(
            filesByExtension,
            filesByDdefFinal,
            excludedFilesByDdefFinal);
    }

    void FilterFilesAndExcludesForDdef(
        IDeploymentDefinition ddef,
        IReadOnlyList<string> ddefFilesForExtension,
        ref List<string> files,
        ref List<string> excludedFiles)
    {
        foreach (var file in ddefFilesForExtension)
        {
            if (DefinitionForPath(file) == ddef)
            {
                if (this.IsPathExcludedByDeploymentDefinition(file, ddef))
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
        IReadOnlyDictionary<string, IReadOnlyList<string>> inputFiles,
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
        IReadOnlyDictionary<string, IReadOnlyList<string>> filesByExtension,
        IReadOnlyDictionary<IDeploymentDefinition, IReadOnlyList<string>> filesByDdef,
        bool isExcludes)
    {
        var fileIntersection = new Dictionary<IDeploymentDefinition, List<string>>();
        foreach (var extensionFiles in filesByExtension.Values)
        {
            foreach (var (ddef, ddefFiles) in filesByDdef)
            {
                if (ddefFiles.Any())
                {
                    var intersection =
                        extensionFiles
                            .Intersect(ddefFiles)
                            .ToList();

                    if (intersection.Any())
                    {
                        if (!fileIntersection.ContainsKey(ddef))
                        {
                            fileIntersection.Add(ddef, new List<string>());
                        }

                        fileIntersection[ddef].AddRange(intersection);
                    }
                }
            }
        }

        if (fileIntersection.Any())
        {
            throw new DeploymentDefinitionFileIntersectionException(fileIntersection, isExcludes);
        }
    }
}
