using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Cli.Authoring.Service;

class DeploymentDefinitionInputResult
{
    public IReadOnlyList<IDeploymentDefinition> InputDeploymentDefinitions { get; }
    public IReadOnlyList<IDeploymentDefinition> AllDeploymentDefinitions { get; }

    public DeploymentDefinitionInputResult(
        IReadOnlyList<IDeploymentDefinition> inputDeploymentDefinitions,
        IReadOnlyList<IDeploymentDefinition> allDeploymentDefinitions)
    {
        InputDeploymentDefinitions = inputDeploymentDefinitions;
        AllDeploymentDefinitions = allDeploymentDefinitions;
    }
}
