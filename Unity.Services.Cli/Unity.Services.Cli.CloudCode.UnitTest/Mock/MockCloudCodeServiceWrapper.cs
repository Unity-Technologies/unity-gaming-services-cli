using Moq;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Deploy.Service;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;

namespace Unity.Services.Cli.CloudCode.UnitTest.Mock;

class MockCloudCodeServiceWrapper : ICloudCodeServicesWrapper
{
    public ICliEnvironmentProvider EnvironmentProvider { get; }
    public IDeployFileService DeployFileService { get; }
    public ICloudCodeScriptsLoader CloudCodeScriptsLoader { get; }
    public ICloudCodeInputParser CloudCodeInputParser { get; }
    public ICloudCodeService CloudCodeService { get; }
    public ICliCloudCodeClient CliCloudCodeClient { get; }
    public ICloudCodeDeploymentHandler CloudCodeDeploymentHandler { get; }
    public ICliDeploymentOutputHandler CliDeploymentOutputHandler { get; }

    public MockCloudCodeServiceWrapper(
        IMock<ICliEnvironmentProvider> mockEnvironmentProvider,
        IMock<CloudCodeScriptsLoader> mockCloudCodeScriptsLoader,
        IMock<IDeployFileService> mockDeployFileService,
        IMock<ICloudCodeInputParser> mockCloudCodeInputParser,
        IMock<ICloudCodeService> mockCloudCodeService,
        IMock<ICliCloudCodeClient> mockCliCloudCodeClient,
        IMock<ICloudCodeDeploymentHandler> mockCloudCodeDeploymentHandler,
        IMock<ICliDeploymentOutputHandler> mockCliDeploymentHandler)
    {
        EnvironmentProvider = mockEnvironmentProvider.Object;
        CloudCodeScriptsLoader = mockCloudCodeScriptsLoader.Object;
        DeployFileService = mockDeployFileService.Object;
        CloudCodeInputParser = mockCloudCodeInputParser.Object;
        CloudCodeService = mockCloudCodeService.Object;
        CliCloudCodeClient = mockCliCloudCodeClient.Object;
        CloudCodeDeploymentHandler = mockCloudCodeDeploymentHandler.Object;
        CliDeploymentOutputHandler = mockCliDeploymentHandler.Object;
    }
}
