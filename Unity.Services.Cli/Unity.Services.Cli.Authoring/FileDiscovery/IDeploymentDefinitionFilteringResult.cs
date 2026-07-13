using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Cli.Authoring.DeploymentDefinition;

interface IDeploymentDefinitionFilteringResult
{
    public IDeploymentDefinitionFiles DefinitionFiles { get; }
    public Dictionary<string, IReadOnlyList<AuthoringFile>> AllFilesByExtension { get; }
    public Dictionary<string, IDeploymentDefinition?> DeploymentDefinitionByInputPath { get; }
    public string GetExclusionsLogMessage();
}
