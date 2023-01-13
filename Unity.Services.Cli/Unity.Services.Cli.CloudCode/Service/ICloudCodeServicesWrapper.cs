using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.Deploy.Service;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;

namespace Unity.Services.Cli.CloudCode.Service;

interface ICloudCodeServicesWrapper
{
    ICloudCodeService CloudCodeService { get; }

    ICloudCodeInputParser CloudCodeInputParser { get; }

    ICliCloudCodeClient CliCloudCodeClient { get; }

    ICloudCodeDeploymentHandler CloudCodeDeploymentHandler { get; }

    ICliEnvironmentProvider EnvironmentProvider { get; }

    ICliDeploymentOutputHandler CliDeploymentOutputHandler { get; }

    ICloudCodeScriptsLoader CloudCodeScriptsLoader { get; }
}
