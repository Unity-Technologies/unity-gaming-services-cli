using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Authoring.Service;

namespace Unity.Services.Cli.CloudCode.UnitTest.Service;

[TestFixture]
class CloudCodeServicesWrapperTests
{
    [Test]
    public void ConstructorKeepsRefs()
    {
        var cloudCodeService = new Mock<ICloudCodeService>();
        var cloudCodeScriptsLoader = new Mock<ICloudCodeScriptsLoader>();
        var deployFileService = new Mock<IDeployFileService>();
        var cloudCodeInputParser = new Mock<ICloudCodeInputParser>();
        var cliCloudCodeClient = new Mock<ICliCloudCodeClient>();
        var cloudCodeDeploymentHandler = new Mock<IDeploymentHandlerWithOutput>();
        var cliDeploymentOutputHandler = cloudCodeDeploymentHandler;
        var cloudCodeModulesLoader = new Mock<ICloudCodeModulesLoader>();

        var environmentProvider = new Mock<ICliEnvironmentProvider>();

        var wrapper = new CloudCodeServicesWrapper(
            cloudCodeService.Object,
            deployFileService.Object,
            cloudCodeScriptsLoader.Object,
            cloudCodeInputParser.Object,
            cliCloudCodeClient.Object,
            cloudCodeDeploymentHandler.Object,
            environmentProvider.Object,
            cloudCodeModulesLoader.Object
        );

        Assert.AreSame(cloudCodeService.Object, wrapper.CloudCodeService);
        Assert.AreSame(deployFileService.Object, wrapper.DeployFileService);
        Assert.AreSame(cloudCodeScriptsLoader.Object, wrapper.CloudCodeScriptsLoader);
        Assert.AreSame(cloudCodeInputParser.Object, wrapper.CloudCodeInputParser);
        Assert.AreSame(cliCloudCodeClient.Object, wrapper.CliCloudCodeClient);
        Assert.AreSame(cloudCodeDeploymentHandler.Object, wrapper.CloudCodeDeploymentHandler);
        Assert.AreSame(cliDeploymentOutputHandler.Object, wrapper.CliDeploymentOutputHandler);
        Assert.AreSame(environmentProvider.Object, wrapper.EnvironmentProvider);
    }
}
