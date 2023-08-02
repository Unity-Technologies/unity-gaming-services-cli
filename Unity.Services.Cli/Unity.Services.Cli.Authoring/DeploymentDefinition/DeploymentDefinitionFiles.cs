using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Cli.Authoring.DeploymentDefinition;

class DeploymentDefinitionFiles : IDeploymentDefinitionFiles
{
    public IReadOnlyDictionary<string, IReadOnlyList<string>> FilesByExtension { get; }
    public IReadOnlyDictionary<IDeploymentDefinition, IReadOnlyList<string>> FilesByDeploymentDefinition { get; }
    public IReadOnlyDictionary<IDeploymentDefinition, IReadOnlyList<string>> ExcludedFilesByDeploymentDefinition { get; }
    public bool HasExcludes => ExcludedFilesByDeploymentDefinition.Any(kvp => kvp.Value.Any());

    public DeploymentDefinitionFiles()
    {
        FilesByExtension = new Dictionary<string, IReadOnlyList<string>>();
        FilesByDeploymentDefinition = new Dictionary<IDeploymentDefinition, IReadOnlyList<string>>();
        ExcludedFilesByDeploymentDefinition = new Dictionary<IDeploymentDefinition, IReadOnlyList<string>>();
    }

    public DeploymentDefinitionFiles(
        IReadOnlyDictionary<string, IReadOnlyList<string>> filesByExtension,
        IReadOnlyDictionary<IDeploymentDefinition, IReadOnlyList<string>> filesByDeploymentDefinition,
        IReadOnlyDictionary<IDeploymentDefinition, IReadOnlyList<string>> excludedFilesByDeploymentDefinition)
    {
        FilesByExtension = filesByExtension;
        FilesByDeploymentDefinition = filesByDeploymentDefinition;
        ExcludedFilesByDeploymentDefinition = excludedFilesByDeploymentDefinition;
    }
}
