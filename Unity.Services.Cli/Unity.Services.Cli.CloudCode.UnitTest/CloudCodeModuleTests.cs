using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.CloudCode.Authoring;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.TestUtils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Analytics;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Logging;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Api;
using IFileSystem = Unity.Services.CloudCode.Authoring.Editor.Core.IO.IFileSystem;

namespace Unity.Services.Cli.CloudCode.UnitTest;

[TestFixture]
class CloudCodeModuleTests
{
    static readonly CloudCodeModule k_CloudCodeModule = new();

    static readonly IEnumerable<TestCaseData> k_CloudCodeCommandTestCases = new[]
    {
        new TestCaseData(k_CloudCodeModule.ModuleRootCommand, k_CloudCodeModule.ScriptsCommand),
        new TestCaseData(k_CloudCodeModule.ScriptsCommand, k_CloudCodeModule.ListCommand),
        new TestCaseData(k_CloudCodeModule.ScriptsCommand, k_CloudCodeModule.DeleteCommand),
        new TestCaseData(k_CloudCodeModule.ScriptsCommand, k_CloudCodeModule.UpdateCommand),
        new TestCaseData(k_CloudCodeModule.ScriptsCommand, k_CloudCodeModule.GetCommand),
        new TestCaseData(k_CloudCodeModule.ScriptsCommand, k_CloudCodeModule.PublishCommand),
        new TestCaseData(k_CloudCodeModule.ScriptsCommand, k_CloudCodeModule.NewFileCommand)
    };

    [Test]
    public void BuildCommands_CreateCommands()
    {
        var commandLineBuilder = new CommandLineBuilder();
        commandLineBuilder.AddModule(k_CloudCodeModule);
        TestsHelper.AssertContainsCommand(
            commandLineBuilder.Command, k_CloudCodeModule.ModuleRootCommand.Name, out var resultCommand);
        Assert.AreEqual(k_CloudCodeModule.ModuleRootCommand, resultCommand);
        Assert.NotNull(k_CloudCodeModule.ListCommand.Handler);
    }

    [Test]
    public void BuildCommands_CreateAliases()
    {
        var commandLineBuilder = new CommandLineBuilder();
        commandLineBuilder.AddModule(k_CloudCodeModule);

        Assert.IsTrue(k_CloudCodeModule.ModuleRootCommand.Aliases.Contains("cc"));
    }

    [Test]
    public void ScriptCommandsContainsAlias()
    {
        var commandLineBuilder = new CommandLineBuilder();
        commandLineBuilder.AddModule(k_CloudCodeModule);

        Assert.IsTrue(k_CloudCodeModule.ScriptsCommand.Aliases.Contains("s"));
    }

    [TestCaseSource(nameof(k_CloudCodeCommandTestCases))]
    public void CommandContainsCommands(Command parentCommand, Command childCommand)
    {
        TestsHelper.AssertContainsCommand(
            parentCommand, childCommand.Name, out var resultCommand);
        Assert.AreEqual(childCommand, resultCommand);
    }

    [Test]
    public void ListCommandWithInput()
    {
        Assert.IsTrue(k_CloudCodeModule.ListCommand.Options.Contains(CommonInput.CloudProjectIdOption));
        Assert.IsTrue(k_CloudCodeModule.ListCommand.Options.Contains(CommonInput.EnvironmentNameOption));
    }

    [Test]
    public void DeleteCommandWithInput()
    {
        Assert.IsTrue(k_CloudCodeModule.DeleteCommand.Options.Contains(CommonInput.CloudProjectIdOption));
        Assert.IsTrue(k_CloudCodeModule.DeleteCommand.Options.Contains(CommonInput.EnvironmentNameOption));
        Assert.IsTrue(k_CloudCodeModule.DeleteCommand.Arguments.Contains(CloudCodeInput.ScriptNameArgument));
    }

    [Test]
    public void CreateCommandWithInput()
    {
        Assert.IsTrue(k_CloudCodeModule.CreateCommand.Options.Contains(CommonInput.CloudProjectIdOption));
        Assert.IsTrue(k_CloudCodeModule.CreateCommand.Options.Contains(CommonInput.EnvironmentNameOption));
        Assert.IsTrue(k_CloudCodeModule.CreateCommand.Options.Contains(CloudCodeInput.ScriptTypeOption));
        Assert.IsTrue(k_CloudCodeModule.CreateCommand.Options.Contains(CloudCodeInput.ScriptLanguageOption));
        Assert.IsTrue(k_CloudCodeModule.CreateCommand.Arguments.Contains(CloudCodeInput.ScriptNameArgument));
        Assert.IsTrue(k_CloudCodeModule.CreateCommand.Arguments.Contains(CloudCodeInput.FilePathArgument));
    }

    [Test]
    public void UpdateCommandWithInput()
    {
        Assert.IsTrue(k_CloudCodeModule.UpdateCommand.Options.Contains(CommonInput.CloudProjectIdOption));
        Assert.IsTrue(k_CloudCodeModule.UpdateCommand.Options.Contains(CommonInput.EnvironmentNameOption));
        Assert.IsTrue(k_CloudCodeModule.UpdateCommand.Arguments.Contains(CloudCodeInput.ScriptNameArgument));
        Assert.IsTrue(k_CloudCodeModule.UpdateCommand.Arguments.Contains(CloudCodeInput.FilePathArgument));
    }

    [Test]
    public void NewFileCommandWithInput()
    {
        Assert.IsTrue(k_CloudCodeModule.NewFileCommand.Arguments.Contains(NewFileInput.FileArgument));
        Assert.IsTrue(k_CloudCodeModule.NewFileCommand.Options.Contains(CommonInput.UseForceOption));
    }

    [TestCase(typeof(ICloudCodeApiAsync))]
    [TestCase(typeof(ICSharpClient))]
    [TestCase(typeof(IJavaScriptClient))]
    [TestCase(typeof(IDeploymentAnalytics))]
    [TestCase(typeof(ILogger))]
    [TestCase(typeof(EnvironmentProvider))]
    [TestCase(typeof(ICliEnvironmentProvider))]
    [TestCase(typeof(IEnvironmentProvider))]
    [TestCase(typeof(IPreDeployValidator))]
    [TestCase(typeof(ICloudCodeModulesLoader))]
    [TestCase(typeof(ICloudCodeScriptsLoader))]
    [TestCase(typeof(ICloudCodeService))]
    [TestCase(typeof(ICloudCodeInputParser))]
    [TestCase(typeof(IConfigurationValidator))]
    [TestCase(typeof(ICloudScriptParametersParser))]
    [TestCase(typeof(ICloudCodeScriptParser))]
    [TestCase(typeof(CliCloudCodeDeploymentHandler<IJavaScriptClient>))]
    [TestCase(typeof(CliCloudCodeDeploymentHandler<ICSharpClient>))]
    public void CloudCodeModuleRegistersServices(Type serviceType)
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(
            new[]
            {
                typeof(CloudCodeEndpoints).GetTypeInfo()
            });
        var services = new List<ServiceDescriptor>();
        var hostBuilder = TestsHelper.CreateAndSetupMockHostBuilder(services);
        hostBuilder.ConfigureServices(CloudCodeModule.RegisterServices);
        Assert.That(services.FirstOrDefault(c => c.ServiceType == serviceType), Is.Not.Null);
    }

    [Test]
    public void CreateJavaScriptDeployServiceCreatesInstanceWithReferencesToProvidedServices()
    {
        var scriptParser = new Mock<ICloudCodeScriptParser>();
        var scriptsLoader = new Mock<ICloudCodeScriptsLoader>();
        var inputParser = new Mock<ICloudCodeInputParser>();
        var environmentProvider = new Mock<ICliEnvironmentProvider>();
        var client = new Mock<IJavaScriptClient>();
        var deploymentHandler = new CliCloudCodeDeploymentHandler<IJavaScriptClient>(
            null!, null!, null!, null!);
        var provider = new Mock<IServiceProvider>();
        SetupProvider();

        var deployService = CloudCodeModule.CreateJavaScriptDeployService(provider.Object);

        Assert.Multiple(AssertExpectedServicesAreWrapped);

        void SetupProvider()
        {
            provider.Setup(x => x.GetService(typeof(ICloudCodeInputParser)))
                .Returns(inputParser.Object);
            provider.Setup(x => x.GetService(typeof(ICloudCodeScriptParser)))
                .Returns(scriptParser.Object);
            provider.Setup(x => x.GetService(typeof(CliCloudCodeDeploymentHandler<IJavaScriptClient>)))
                .Returns(deploymentHandler);
            provider.Setup(x => x.GetService(typeof(ICloudCodeScriptsLoader)))
                .Returns(scriptsLoader.Object);
            provider.Setup(x => x.GetService(typeof(ICliEnvironmentProvider)))
                .Returns(environmentProvider.Object);
            provider.Setup(x => x.GetService(typeof(IJavaScriptClient)))
                .Returns(client.Object);
        }

        void AssertExpectedServicesAreWrapped()
        {
            Assert.That(deployService.CloudCodeInputParser, Is.SameAs(inputParser.Object));
            Assert.That(deployService.CloudCodeScriptParser, Is.SameAs(scriptParser.Object));
            Assert.That(deployService.CloudCodeScriptsLoader, Is.SameAs(scriptsLoader.Object));
            Assert.That(deployService.EnvironmentProvider, Is.SameAs(environmentProvider.Object));
            Assert.That(deployService.CliCloudCodeClient, Is.SameAs(client.Object));
        }
    }

    [Test]
    public void CreateCSharpDeployServiceCreatesInstanceWithReferencesToProvidedServices()
    {
        var environmentProvider = new Mock<ICliEnvironmentProvider>();
        var csModuleLoader = new Mock<ICloudCodeModulesLoader>();
        var client = new Mock<ICSharpClient>();
        var solutionPublisher = new Mock<ISolutionPublisher>();
        var moduleSZipper = new Mock<IModuleZipper>();
        var fileSystem = new Mock<IFileSystem>();
        var fileService = new Mock<IDeployFileService>();
        var deploymentHandlerWithOutput = new CliCloudCodeDeploymentHandler<ICSharpClient>(
            client.Object, null!, null!, null!);
        var provider = new Mock<IServiceProvider>();
        SetupProvider();

        var deployService = CloudCodeModule.CreateCSharpDeployService(provider.Object);

        Assert.Multiple(AssertExpectedServicesAreWrapped);

        void SetupProvider()
        {
            provider.Setup(x => x.GetService(typeof(CliCloudCodeDeploymentHandler<ICSharpClient>)))
                .Returns(deploymentHandlerWithOutput);
            provider.Setup(x => x.GetService(typeof(ICloudCodeModulesLoader)))
                .Returns(csModuleLoader.Object);
            provider.Setup(x => x.GetService(typeof(ICliEnvironmentProvider)))
                .Returns(environmentProvider.Object);
            provider.Setup(x => x.GetService(typeof(ICSharpClient)))
                .Returns(client.Object);
            provider.Setup(x => x.GetService(typeof(IDeployFileService)))
                .Returns(fileService.Object);
            provider.Setup(x => x.GetService(typeof(ISolutionPublisher)))
                .Returns(solutionPublisher.Object);
            provider.Setup(x => x.GetService(typeof(IModuleZipper)))
                .Returns(moduleSZipper.Object);
            provider.Setup(x => x.GetService(typeof(IFileSystem)))
                .Returns(fileSystem.Object);
        }

        void AssertExpectedServicesAreWrapped()
        {
            Assert.That(deployService.CloudCodeModulesLoader, Is.SameAs(csModuleLoader.Object));
            Assert.That(deployService.EnvironmentProvider, Is.SameAs(environmentProvider.Object));
            Assert.That(deployService.CliCloudCodeClient, Is.SameAs(client.Object));
        }
    }
}
