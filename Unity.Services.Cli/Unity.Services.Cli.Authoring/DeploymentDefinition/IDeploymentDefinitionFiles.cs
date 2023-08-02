using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Cli.Authoring.DeploymentDefinition;

interface IDeploymentDefinitionFiles
{
    public IReadOnlyDictionary<string, IReadOnlyList<string>> FilesByExtension { get; }
    public IReadOnlyDictionary<IDeploymentDefinition, IReadOnlyList<string>> FilesByDeploymentDefinition { get; }
    public IReadOnlyDictionary<IDeploymentDefinition, IReadOnlyList<string>> ExcludedFilesByDeploymentDefinition { get; }
    public bool HasExcludes { get; }
}
