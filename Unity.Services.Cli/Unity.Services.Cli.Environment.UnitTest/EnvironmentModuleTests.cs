using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Environment.Input;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.IdentityApiV1.Generated.Api;

namespace Unity.Services.Cli.Environment.UnitTest;

[TestFixture]
class EnvironmentModuleTests
{
    static readonly EnvironmentModule k_EnvironmentModule = new();

    static readonly IEnumerable<TestCaseData> k_EnvironmentCommandTestCases = new[]
    {
        new TestCaseData(k_EnvironmentModule.ListCommand),
        new TestCaseData(k_EnvironmentModule.AddCommand),
        new TestCaseData(k_EnvironmentModule.DeleteCommand),
        new TestCaseData(k_EnvironmentModule.UseCommand)
    };

    [Test]
    public void BuildEnvironmentCommands_CreateEnvironmentCommands()
    {
        var commandLineBuilder = new CommandLineBuilder();
        commandLineBuilder.AddModule(k_EnvironmentModule);
        TestsHelper.AssertContainsCommand(commandLineBuilder.Command, k_EnvironmentModule.ModuleRootCommand.Name, out var resultCommand);
        Assert.AreEqual(k_EnvironmentModule.ModuleRootCommand, resultCommand);
        Assert.NotNull(k_EnvironmentModule.ListCommand.Handler);
        Assert.NotNull(k_EnvironmentModule.AddCommand.Handler);
        Assert.NotNull(k_EnvironmentModule.DeleteCommand.Handler);
        Assert.NotNull(k_EnvironmentModule.UseCommand.Handler);
    }

    [TestCaseSource(nameof(k_EnvironmentCommandTestCases))]
    public void EnvCommandContainCommands(Command expectedCommand)
    {
        TestsHelper.AssertContainsCommand(k_EnvironmentModule.ModuleRootCommand, expectedCommand.Name, out var resultCommand);
        Assert.AreEqual(expectedCommand, resultCommand);
    }

    [Test]
    public void ListCommandWithInput()
    {
        Assert.IsTrue(k_EnvironmentModule.ListCommand.Options.Contains(CommonInput.CloudProjectIdOption));
    }

    [Test]
    public void AddCommandWithInput()
    {
        Assert.IsTrue(k_EnvironmentModule.AddCommand.Options.Contains(CommonInput.CloudProjectIdOption));
        Assert.IsTrue(k_EnvironmentModule.AddCommand.Arguments.Contains(EnvironmentInput.EnvironmentNameArgument));
    }

    [Test]
    public void DeleteCommandWithInput()
    {
        Assert.IsTrue(k_EnvironmentModule.DeleteCommand.Options.Contains(CommonInput.CloudProjectIdOption));
        Assert.IsTrue(k_EnvironmentModule.DeleteCommand.Arguments.Contains(EnvironmentInput.EnvironmentNameArgument));
    }

    [Test]
    public void UseCommandWithInput()
    {
        Assert.IsTrue(k_EnvironmentModule.UseCommand.Arguments.Contains(EnvironmentInput.EnvironmentNameArgument));
    }

    [Test]
    public void ConfigureEnvironmentRegistersExpectedServices()
    {
        var services = new List<ServiceDescriptor>
        {
            ServiceDescriptor.Singleton(new Mock<IEnvironmentApi>().Object),
            ServiceDescriptor.Singleton(new Mock<IServiceAccountAuthenticationService>().Object),
        };
        var hostBuilder = TestsHelper.CreateAndSetupMockHostBuilder(services);

        hostBuilder.ConfigureServices(EnvironmentModule.RegisterServices);
        Assert.AreEqual(4, services.Count);
        TestsHelper.AssertHasServiceSingleton<IEnvironmentService, EnvironmentService>(services);
        TestsHelper.AssertHasServiceSingleton<IUnityEnvironment, UnityEnvironment>(services);
    }
}
