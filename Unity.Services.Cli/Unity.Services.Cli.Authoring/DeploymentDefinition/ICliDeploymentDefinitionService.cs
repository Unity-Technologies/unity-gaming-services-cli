using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Deployment.Core;

namespace Unity.Services.Cli.Authoring.Service;

interface ICliDeploymentDefinitionService : IDeploymentDefinitionService
{
    IDeploymentDefinitionFilteringResult GetFilesFromInput(
        IEnumerable<string> inputPaths,
        IEnumerable<string> extensions);
}
