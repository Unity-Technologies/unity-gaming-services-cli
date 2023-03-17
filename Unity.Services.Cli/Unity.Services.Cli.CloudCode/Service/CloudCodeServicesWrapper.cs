using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;

namespace Unity.Services.Cli.CloudCode.Service;

class CloudCodeServicesWrapper : ICloudCodeServicesWrapper
{
    public ICloudCodeService CloudCodeService { get; }
    public ICloudCodeInputParser CloudCodeInputParser { get; }
    public ICliCloudCodeClient CliCloudCodeClient { get; }
    public ICloudCodeDeploymentHandler CloudCodeDeploymentHandler { get; }
    public ICliEnvironmentProvider EnvironmentProvider { get; }
    public ICliDeploymentOutputHandler CliDeploymentOutputHandler { get; }
    public IDeployFileService DeployFileService { get; }
    public ICloudCodeScriptsLoader CloudCodeScriptsLoader { get; }
    public ICloudCodeModulesLoader CloudCodeModulesLoader { get; }

    public CloudCodeServicesWrapper(
        ICloudCodeService cloudCodeService,
        IDeployFileService deployFileService,
        ICloudCodeScriptsLoader cloudCodeScriptsLoader,
        ICloudCodeInputParser cloudCodeInputParser,
        ICliCloudCodeClient cliCloudCodeClient,
        IDeploymentHandlerWithOutput deploymentHandlerWithOutput,
        ICliEnvironmentProvider environmentProvider,
        ICloudCodeModulesLoader cloudCodeModulesLoader
    )
    {
        CloudCodeService = cloudCodeService;
        DeployFileService = deployFileService;
        CloudCodeScriptsLoader = cloudCodeScriptsLoader;
        CloudCodeInputParser = cloudCodeInputParser;
        CliCloudCodeClient = cliCloudCodeClient;
        CloudCodeDeploymentHandler = deploymentHandlerWithOutput;
        EnvironmentProvider = environmentProvider;
        CliDeploymentOutputHandler = deploymentHandlerWithOutput;
        CloudCodeModulesLoader = cloudCodeModulesLoader;
    }
}
