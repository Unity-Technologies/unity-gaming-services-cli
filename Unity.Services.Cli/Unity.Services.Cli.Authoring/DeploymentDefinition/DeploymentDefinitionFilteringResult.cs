using System.Text;
using Unity.Services.Cli.Authoring.DeploymentDefinition;

namespace Unity.Services.Cli.Authoring.Service;

class DeploymentDefinitionFilteringResult : IDeploymentDefinitionFilteringResult
{
    public IDeploymentDefinitionFiles DefinitionFiles { get; }
    public Dictionary<string, IReadOnlyList<string>> AllFilesByExtension { get; }

    public DeploymentDefinitionFilteringResult(
        IDeploymentDefinitionFiles definitionFiles,
        Dictionary<string, IReadOnlyList<string>> allFilesByExtension)
    {
        DefinitionFiles = definitionFiles;
        AllFilesByExtension = allFilesByExtension;
    }

    public string GetExclusionsLogMessage()
    {
        var sb = new StringBuilder();
        sb.Append("The following files were excluded by deployment definitions:");
        foreach (var (ddef, excludedFiles) in DefinitionFiles.ExcludedFilesByDeploymentDefinition)
        {
            foreach (var file in excludedFiles)
            {
                sb.Append($"{Environment.NewLine}\t'{file}' [{ddef.Name}.ddef]");
            }
        }

        return sb.ToString();
    }
}
