using Unity.Services.Deployment.Core.Model;
using Unity.Services.Cli.Authoring.Model;

namespace Unity.Services.Cli.Authoring.DeploymentDefinition;

class DeploymentDefinitionFiles : IDeploymentDefinitionFiles
{
    public IReadOnlyDictionary<string, IReadOnlyList<AuthoringFile>> FilesByExtension { get; }
    public IReadOnlyDictionary<IDeploymentDefinition, IReadOnlyList<AuthoringFile>> FilesByDeploymentDefinition { get; }
    public IReadOnlyDictionary<IDeploymentDefinition, IReadOnlyList<AuthoringFile>> ExcludedFilesByDeploymentDefinition { get; }
    public bool HasExcludes => ExcludedFilesByDeploymentDefinition.Any(kvp => kvp.Value.Any());

    public DeploymentDefinitionFiles()
    {
        FilesByExtension = new Dictionary<string, IReadOnlyList<AuthoringFile>>();
        FilesByDeploymentDefinition = new Dictionary<IDeploymentDefinition, IReadOnlyList<AuthoringFile>>();
        ExcludedFilesByDeploymentDefinition = new Dictionary<IDeploymentDefinition, IReadOnlyList<AuthoringFile>>();
    }

    public DeploymentDefinitionFiles(
        IReadOnlyDictionary<string, IReadOnlyList<AuthoringFile>> filesByExtension,
        IReadOnlyDictionary<IDeploymentDefinition, IReadOnlyList<AuthoringFile>> filesByDeploymentDefinition,
        IReadOnlyDictionary<IDeploymentDefinition, IReadOnlyList<AuthoringFile>> excludedFilesByDeploymentDefinition)
    {
        FilesByExtension = filesByExtension;
        FilesByDeploymentDefinition = filesByDeploymentDefinition;
        ExcludedFilesByDeploymentDefinition = excludedFilesByDeploymentDefinition;
    }
}
