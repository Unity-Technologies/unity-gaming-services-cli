using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Cli.Authoring.DeploymentDefinition;

public class MultipleDeploymentDefinitionInDirectoryException : Exception
{
    internal MultipleDeploymentDefinitionInDirectoryException(
        IDeploymentDefinition ddef1,
        IDeploymentDefinition ddef2,
        string path)
        : base($"Multiple deployment definitions were found in the directory '{path}': '{ddef1.Name}.ddef' and '{ddef2.Name}.ddef'")
    {
    }
}
