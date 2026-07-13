using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Deployment.Core;

namespace Unity.Services.Cli.Authoring.Service;

interface IAuthoringFileService : IDeploymentDefinitionService
{
    IDeploymentDefinitionFilteringResult ResolveAuthoringFiles(
        IReadOnlyList<string> inputPaths,
        IReadOnlyList<string> extensions);
}
