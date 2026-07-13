using Unity.Services.Deployment.Core.Model;
using Unity.Services.Cli.Authoring.Model;

namespace Unity.Services.Cli.Authoring.Service;

interface IFileDiscoveryService : IDeployFileService
{
    /// <summary>
    /// Discovers all deployment definitions reachable from the given input paths.
    /// This includes ddefs found directly in the input, as well as parent and child ddefs.
    /// Does not classify which ddefs were explicitly provided as input.
    /// </summary>
    IReadOnlyList<IDeploymentDefinition> DiscoverDeploymentDefinitions(IEnumerable<string> inputPaths);

    IReadOnlyList<AuthoringFile> GetFilesForDeploymentDefinition(
        IDeploymentDefinition deploymentDefinition,
        string extension);
}
