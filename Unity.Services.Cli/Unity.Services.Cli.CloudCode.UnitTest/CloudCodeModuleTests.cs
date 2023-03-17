using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
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
using Unity.Services.CloudCode.Authoring.Editor.Core.Crypto;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Api;

namespace Unity.Services.Cli.CloudCode.UnitTest;

[TestFixture]
class CloudCodeModuleTests
{
    static readonly CloudCodeModule k_CloudCodeModule = new();

    static readonly IEnumerable<TestCaseData> k_CloudCodeCommandTestCases = new[]
    {
        new TestCaseData(k_CloudCodeModule.ListCommand),
        new TestCaseData(k_CloudCodeModule.DeleteCommand)
    };

    [Test]
    public void BuildCommands_CreateCommands()
    {
        var commandLineBuilder = new CommandLineBuilder();
        commandLineBuilder.AddModule(k_CloudCodeModule);
        TestsHelper.AssertContainsCommand(commandLineBuilder.Command, k_CloudCodeModule.ModuleRootCommand.Name,
            out var resultCommand);
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

    [TestCaseSource(nameof(k_CloudCodeCommandTestCases))]
    public void CommandContainCommands(Command expectedCommand)
    {
        TestsHelper.AssertContainsCommand(k_CloudCodeModule.ModuleRootCommand, expectedCommand.Name,
            out var resultCommand);
        Assert.AreEqual(expectedCommand, resultCommand);
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
        Assert.IsTrue(k_CloudCodeModule.CreateCommand.Options.Contains(CommonInput.CloudProjectIdOption));
        Assert.IsTrue(k_CloudCodeModule.CreateCommand.Options.Contains(CommonInput.EnvironmentNameOption));
        Assert.IsTrue(k_CloudCodeModule.CreateCommand.Arguments.Contains(CloudCodeInput.ScriptNameArgument));
        Assert.IsTrue(k_CloudCodeModule.CreateCommand.Arguments.Contains(CloudCodeInput.FilePathArgument));
    }

    [TestCase(typeof(IConfigurationValidator))]
    [TestCase(typeof(ICloudCodeApiAsync))]
    [TestCase(typeof(ICloudScriptParametersParser))]
    [TestCase(typeof(ICloudCodeScriptParser))]
    [TestCase(typeof(ICloudCodeService))]
    [TestCase(typeof(ICloudCodeInputParser))]
    [TestCase(typeof(IDeploymentAnalytics))]
    [TestCase(typeof(Unity.Services.CloudCode.Authoring.Editor.Core.Logging.ILogger))]
    [TestCase(typeof(EnvironmentProvider))]
    [TestCase(typeof(ICliEnvironmentProvider))]
    [TestCase(typeof(IEnvironmentProvider))]
    [TestCase(typeof(IHashComputer))]
    [TestCase(typeof(IScriptCache))]
    [TestCase(typeof(IPreDeployValidator))]
    [TestCase(typeof(CliCloudCodeDeploymentHandler))]
    public void CloudCodeModuleRegistersServices(Type serviceType)
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(new[]
        {
            typeof(CloudCodeEndpoints).GetTypeInfo()
        });
        var services = new List<ServiceDescriptor>();
        var hostBuilder = TestsHelper.CreateAndSetupMockHostBuilder(services);
        hostBuilder.ConfigureServices(CloudCodeModule.RegisterServices);
        Assert.That(services.FirstOrDefault(c => c.ServiceType == serviceType), Is.Not.Null);
    }
}
