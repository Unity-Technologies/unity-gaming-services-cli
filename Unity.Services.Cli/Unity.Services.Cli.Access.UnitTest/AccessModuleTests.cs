using System.CommandLine.Builder;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Unity.Services.Cli.Access.Input;
using Unity.Services.Cli.Access.Service;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.AccessApiV1.Generated.Api;

namespace Unity.Services.Cli.Access.UnitTest;

[TestFixture]
public class AccessModuleTests
{
    private readonly AccessModule k_AccessModule = new();

    [Test]
    public void BuildCommands_CreateCommands()
    {
        var commandLineBuilder = new CommandLineBuilder();
        commandLineBuilder.AddModule(k_AccessModule);
        TestsHelper.AssertContainsCommand(commandLineBuilder.Command, k_AccessModule.ModuleRootCommand!.Name, out var resultCommand);
        Assert.Multiple(() =>
        {
            Assert.That(resultCommand, Is.EqualTo(k_AccessModule.ModuleRootCommand));
            Assert.That(k_AccessModule.GetPlayerPolicyCommand!.Handler, Is.Not.Null);
            Assert.That(k_AccessModule.GetProjectPolicyCommand!.Handler, Is.Not.Null);
            Assert.That(k_AccessModule.GetAllPlayerPoliciesCommand!.Handler, Is.Not.Null);
            Assert.That(k_AccessModule.UpsertProjectPolicyCommand!.Handler, Is.Not.Null);
            Assert.That(k_AccessModule.UpsertPlayerPolicyCommand!.Handler, Is.Not.Null);
            Assert.That(k_AccessModule.DeleteProjectPolicyStatementsCommand!.Handler, Is.Not.Null);
            Assert.That(k_AccessModule.ModuleRootCommand!.Aliases, Does.Contain("ac"));
            Assert.That(k_AccessModule.DeletePlayerPolicyStatementsCommand!.Handler, Is.Not.Null);
        });
    }

    [Test]
    public void GetProjectPolicyCommand_ContainsRequiredInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(k_AccessModule.GetProjectPolicyCommand!.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(k_AccessModule.GetProjectPolicyCommand!.Options, Does.Contain(CommonInput.EnvironmentNameOption));
        });
    }

    [Test]
    public void GetPlayerPolicyCommand_ContainsRequiredInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(k_AccessModule.GetPlayerPolicyCommand!.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(k_AccessModule.GetPlayerPolicyCommand!.Options, Does.Contain(CommonInput.EnvironmentNameOption));
            Assert.That(k_AccessModule.GetPlayerPolicyCommand.Arguments, Does.Contain(AccessInput.PlayerIdArgument));
        });
    }

    [Test]
    public void GetAllPlayerPoliciesCommand_ContainsRequiredInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(k_AccessModule.GetAllPlayerPoliciesCommand!.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(k_AccessModule.GetAllPlayerPoliciesCommand!.Options, Does.Contain(CommonInput.EnvironmentNameOption));
        });
    }

    [Test]
    public void UpsertProjectPolicyCommand_ContainsRequiredInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(k_AccessModule.UpsertProjectPolicyCommand!.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(k_AccessModule.UpsertProjectPolicyCommand!.Options, Does.Contain(CommonInput.EnvironmentNameOption));
            Assert.That(k_AccessModule.UpsertProjectPolicyCommand!.Arguments, Does.Contain(AccessInput.FilePathArgument));
        });
    }

    [Test]
    public void UpsertPlayerPolicyCommand_ContainsRequiredInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(k_AccessModule.UpsertPlayerPolicyCommand!.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(k_AccessModule.UpsertPlayerPolicyCommand!.Options, Does.Contain(CommonInput.EnvironmentNameOption));
            Assert.That(k_AccessModule.UpsertPlayerPolicyCommand!.Arguments, Does.Contain(AccessInput.PlayerIdArgument));
            Assert.That(k_AccessModule.UpsertPlayerPolicyCommand!.Arguments, Does.Contain(AccessInput.FilePathArgument));
        });
    }

    [Test]
    public void DeleteProjectPolicyStatements_ContainsRequiredInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(k_AccessModule.DeleteProjectPolicyStatementsCommand!.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(k_AccessModule.DeleteProjectPolicyStatementsCommand!.Options, Does.Contain(CommonInput.EnvironmentNameOption));
            Assert.That(k_AccessModule.DeleteProjectPolicyStatementsCommand!.Arguments, Does.Contain(AccessInput.FilePathArgument));
        });
    }

    [Test]
    public void DeletePlayerPolicyStatements_ContainsRequiredInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(k_AccessModule.DeletePlayerPolicyStatementsCommand!.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(k_AccessModule.DeletePlayerPolicyStatementsCommand!.Options, Does.Contain(CommonInput.EnvironmentNameOption));
            Assert.That(k_AccessModule.DeletePlayerPolicyStatementsCommand!.Arguments, Does.Contain(AccessInput.PlayerIdArgument));
            Assert.That(k_AccessModule.DeletePlayerPolicyStatementsCommand!.Arguments, Does.Contain(AccessInput.FilePathArgument));
        });
    }

    [TestCase(typeof(IProjectPolicyApi))]
    [TestCase(typeof(IPlayerPolicyApi))]
    [TestCase(typeof(IAccessService))]
    public void AccessModuleModuleRegistersServices(Type serviceType)
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(new[]
        {
            typeof(AccessEndpoints).GetTypeInfo()
        });
        var services = new List<ServiceDescriptor>();
        var hostBuilder = TestsHelper.CreateAndSetupMockHostBuilder(services);
        hostBuilder.ConfigureServices(AccessModule.RegisterServices);
        Assert.That(services.FirstOrDefault(c => c.ServiceType == serviceType), Is.Not.Null);
    }
}
