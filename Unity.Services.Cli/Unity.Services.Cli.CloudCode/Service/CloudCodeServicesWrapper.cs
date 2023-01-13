using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.Deploy.Service;
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

    public CloudCodeServicesWrapper(
        ICloudCodeService cloudCodeService,
        IDeployFileService deployFileService,
        ICloudCodeScriptsLoader cloudCodeScriptsLoader,
        ICloudCodeInputParser cloudCodeInputParser,
        ICliCloudCodeClient cliCloudCodeClient,
        ICloudCodeDeploymentHandler cloudCodeDeploymentHandler,
        ICliDeploymentOutputHandler cliDeploymentOutputHandler,
        ICliEnvironmentProvider environmentProvider)
    {
        CloudCodeService = cloudCodeService;
        DeployFileService = deployFileService;
        CloudCodeScriptsLoader = cloudCodeScriptsLoader;
        CloudCodeInputParser = cloudCodeInputParser;
        CliCloudCodeClient = cliCloudCodeClient;
        CloudCodeDeploymentHandler = cloudCodeDeploymentHandler;
        EnvironmentProvider = environmentProvider;
        CliDeploymentOutputHandler = cliDeploymentOutputHandler;
    }
}
