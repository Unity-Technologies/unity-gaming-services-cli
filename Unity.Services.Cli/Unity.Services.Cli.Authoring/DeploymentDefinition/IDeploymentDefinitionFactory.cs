using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Cli.Authoring.DeploymentDefinition;

interface IDeploymentDefinitionFactory
{
    IDeploymentDefinition CreateDeploymentDefinition(string path);
}
