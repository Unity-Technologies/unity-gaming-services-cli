using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Cli.Authoring.Service;

interface IDeploymentDefinitionFileService : IDeployFileService
{
    DeploymentDefinitionInputResult GetDeploymentDefinitionsForInput(IEnumerable<string> inputPaths);
    IReadOnlyList<string> GetFilesForDeploymentDefinition(
        IDeploymentDefinition deploymentDefinition,
        string extension);
}
