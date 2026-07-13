using Unity.Services.Deployment.Core.Model;
using Unity.Services.Cli.Authoring.Model;

namespace Unity.Services.Cli.Authoring.DeploymentDefinition;

interface IDeploymentDefinitionFiles
{
    public IReadOnlyDictionary<string, IReadOnlyList<AuthoringFile>> FilesByExtension { get; }
    public IReadOnlyDictionary<IDeploymentDefinition, IReadOnlyList<AuthoringFile>> FilesByDeploymentDefinition { get; }
    public IReadOnlyDictionary<IDeploymentDefinition, IReadOnlyList<AuthoringFile>> ExcludedFilesByDeploymentDefinition { get; }
    public bool HasExcludes { get; }
}
