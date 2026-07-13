using System.Text;
using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Cli.Authoring.Service;

class DeploymentDefinitionFilteringResult : IDeploymentDefinitionFilteringResult
{
    public IDeploymentDefinitionFiles DefinitionFiles { get; }
    public Dictionary<string, IReadOnlyList<AuthoringFile>> AllFilesByExtension { get; }
    public Dictionary<string, IDeploymentDefinition?> DeploymentDefinitionByInputPath { get; }

    public DeploymentDefinitionFilteringResult(
        IDeploymentDefinitionFiles definitionFiles,
        Dictionary<string, IReadOnlyList<AuthoringFile>> allFilesByExtension,
        Dictionary<string, IDeploymentDefinition?> deploymentDefinitionByInputPath)
    {
        DefinitionFiles = definitionFiles;
        AllFilesByExtension = allFilesByExtension;
        DeploymentDefinitionByInputPath = deploymentDefinitionByInputPath;
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
