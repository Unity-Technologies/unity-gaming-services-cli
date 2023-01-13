using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Deploy.Service;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;

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
        var cloudCodeDeploymentHandler = new Mock<ICloudCodeDeploymentHandler>();
        var cliDeploymentOutputHandler = new Mock<ICliDeploymentOutputHandler>();

        var environmentProvider = new Mock<ICliEnvironmentProvider>();

        var wrapper = new CloudCodeServicesWrapper(
            cloudCodeService.Object,
            deployFileService.Object,
            cloudCodeScriptsLoader.Object,
            cloudCodeInputParser.Object,
            cliCloudCodeClient.Object,
            cloudCodeDeploymentHandler.Object,
            cliDeploymentOutputHandler.Object,
            environmentProvider.Object);

        Assert.AreSame(cloudCodeService.Object, wrapper.CloudCodeService);
        Assert.AreSame(deployFileService.Object, wrapper.DeployFileService);
        Assert.AreSame(cloudCodeInputParser.Object, wrapper.CloudCodeInputParser);
        Assert.AreSame(cliCloudCodeClient.Object, wrapper.CliCloudCodeClient);
        Assert.AreSame(cloudCodeDeploymentHandler.Object, wrapper.CloudCodeDeploymentHandler);
        Assert.AreSame(cliDeploymentOutputHandler.Object, wrapper.CliDeploymentOutputHandler);
        Assert.AreSame(environmentProvider.Object, wrapper.EnvironmentProvider);
    }
}
