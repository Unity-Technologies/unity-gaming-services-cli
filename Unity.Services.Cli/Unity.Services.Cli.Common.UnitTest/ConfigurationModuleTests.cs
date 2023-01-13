using System.CommandLine;
using System.CommandLine.Builder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Unity.Services.Cli.Common.Persister;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Common.UnitTest;

[TestFixture]
class ConfigurationModuleTests
{
    static readonly ConfigurationModule k_ConfigModule = new();

    static readonly IEnumerable<TestCaseData> k_ConfigCommandTestCases = new[]
    {
        new TestCaseData(k_ConfigModule.GetCommand),
        new TestCaseData(k_ConfigModule.SetCommand)
    };

    [TestCaseSource(nameof(k_ConfigCommandTestCases))]
    public void ConfigCommandContainCommands(Command expectedCommand)
    {
        TestsHelper.AssertContainsCommand(k_ConfigModule.ModuleRootCommand,
            expectedCommand.Name, out var resultCommand);
        Assert.AreSame(expectedCommand, resultCommand);
    }

    [Test]
    public void BuildConfigurationCommandsCreatesCommand()
    {
        var root = new RootCommand("Test root command");
        var builder = new CommandLineBuilder(root);

        builder.AddModule(k_ConfigModule);
        TestsHelper.AssertContainsCommand(builder.Command, k_ConfigModule.ModuleRootCommand.Name, out var resultCommand);
        Assert.AreSame(k_ConfigModule.ModuleRootCommand, resultCommand);
        Assert.NotNull(k_ConfigModule.GetCommand.Handler);
        Assert.NotNull(k_ConfigModule.SetCommand.Handler);
    }

    [Test]
    public void GetCommandWithInput()
    {
        Assert.IsTrue(k_ConfigModule.GetCommand.Arguments.Contains(ConfigurationInput.KeyArgument));
    }

    [Test]
    public void SetCommandWithInput()
    {
        Assert.IsTrue(k_ConfigModule.SetCommand.Arguments.Contains(ConfigurationInput.KeyArgument));
        Assert.IsTrue(k_ConfigModule.SetCommand.Arguments.Contains(ConfigurationInput.ValueArgument));
    }

    [Test]
    public void ConfigureEnvironmentRegistersExpectedServices()
    {
        var services = new List<ServiceDescriptor>();
        var hostBuilder = TestsHelper.CreateAndSetupMockHostBuilder(services);

        hostBuilder.ConfigureServices(ConfigurationModule.RegisterServices);
        Assert.AreEqual(3, services.Count);
        TestsHelper.AssertHasServiceSingleton<IConfigurationValidator, ConfigurationValidator>(services);
        TestsHelper.AssertHasServiceSingleton<IConfigurationService, ConfigurationService>(services);
        TestsHelper.AssertHasServiceSingleton<IPersister<Models.Configuration>, JsonFilePersister<Models.Configuration>>(services);
    }
}
