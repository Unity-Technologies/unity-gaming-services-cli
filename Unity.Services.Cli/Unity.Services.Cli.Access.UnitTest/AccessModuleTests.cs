using System.CommandLine.Builder;
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
    readonly AccessModule m_AccessModule = new();

    [Test]
    public void BuildCommands_CreateCommands()
    {
        var commandLineBuilder = new CommandLineBuilder();
        commandLineBuilder.AddModule(m_AccessModule);
        TestsHelper.AssertContainsCommand(commandLineBuilder.Command, m_AccessModule.ModuleRootCommand!.Name, out var resultCommand);
        Assert.Multiple(() =>
        {
            Assert.That(resultCommand, Is.EqualTo(m_AccessModule.ModuleRootCommand));
            Assert.That(m_AccessModule.ProjectPolicyListCommand!.Handler, Is.Not.Null);
            Assert.That(m_AccessModule.ProjectPolicyDeleteCommand!.Handler, Is.Not.Null);
            Assert.That(m_AccessModule.PlayerPolicyListCommand!.Handler, Is.Not.Null);
            Assert.That(m_AccessModule.PlayerPolicyUpdateCommand!.Handler, Is.Not.Null);
            Assert.That(m_AccessModule.PlayerPolicyDeleteCommand!.Handler, Is.Not.Null);
            Assert.That(m_AccessModule.ModuleRootCommand!.Aliases, Does.Contain("ac"));
        });
    }

    [Test]
    public void ModuleRootCommand_ContainsProjectPolicyAndPlayerPolicySubcommands()
    {
        Assert.Multiple(() =>
        {
            Assert.That(m_AccessModule.ModuleRootCommand!.Subcommands, Does.Contain(m_AccessModule.ProjectPolicyCommand));
            Assert.That(m_AccessModule.ModuleRootCommand!.Subcommands, Does.Contain(m_AccessModule.PlayerPolicyCommand));
        });
    }

    [Test]
    public void ProjectPolicyCommand_ContainsExpectedSubcommands()
    {
        Assert.Multiple(() =>
        {
            Assert.That(m_AccessModule.ProjectPolicyCommand!.Subcommands, Does.Contain(m_AccessModule.ProjectPolicyListCommand));
            Assert.That(m_AccessModule.ProjectPolicyCommand!.Subcommands, Does.Contain(m_AccessModule.ProjectPolicyDeleteCommand));
        });
    }

    [Test]
    public void PlayerPolicyCommand_ContainsExpectedSubcommands()
    {
        Assert.Multiple(() =>
        {
            Assert.That(m_AccessModule.PlayerPolicyCommand!.Subcommands, Does.Contain(m_AccessModule.PlayerPolicyListCommand));
            Assert.That(m_AccessModule.PlayerPolicyCommand!.Subcommands, Does.Contain(m_AccessModule.PlayerPolicyUpdateCommand));
            Assert.That(m_AccessModule.PlayerPolicyCommand!.Subcommands, Does.Contain(m_AccessModule.PlayerPolicyDeleteCommand));
        });
    }

    [Test]
    public void ProjectPolicyListCommand_ContainsRequiredInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(m_AccessModule.ProjectPolicyListCommand!.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(m_AccessModule.ProjectPolicyListCommand!.Options, Does.Contain(CommonInput.EnvironmentNameOption));
        });
    }

    [Test]
    public void ProjectPolicyDeleteCommand_ContainsRequiredInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(m_AccessModule.ProjectPolicyDeleteCommand!.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(m_AccessModule.ProjectPolicyDeleteCommand!.Options, Does.Contain(CommonInput.EnvironmentNameOption));
            Assert.That(m_AccessModule.ProjectPolicyDeleteCommand!.Arguments, Does.Contain(AccessInput.StatementIdsArgument));
        });
    }

    [Test]
    public void PlayerPolicyListCommand_ContainsRequiredInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(m_AccessModule.PlayerPolicyListCommand!.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(m_AccessModule.PlayerPolicyListCommand!.Options, Does.Contain(CommonInput.EnvironmentNameOption));
            Assert.That(m_AccessModule.PlayerPolicyListCommand!.Options, Does.Contain(PlayerPolicyListInput.PlayerIdOption));
        });
    }

    [Test]
    public void PlayerPolicyUpdateCommand_ContainsRequiredInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(m_AccessModule.PlayerPolicyUpdateCommand!.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(m_AccessModule.PlayerPolicyUpdateCommand!.Options, Does.Contain(CommonInput.EnvironmentNameOption));
            Assert.That(m_AccessModule.PlayerPolicyUpdateCommand!.Arguments, Does.Contain(PlayerPolicyInput.PlayerIdArgument));
            Assert.That(m_AccessModule.PlayerPolicyUpdateCommand!.Arguments, Does.Contain(AccessInput.FilePathArgument));
        });
    }

    [Test]
    public void PlayerPolicyDeleteCommand_ContainsRequiredInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(m_AccessModule.PlayerPolicyDeleteCommand!.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(m_AccessModule.PlayerPolicyDeleteCommand!.Options, Does.Contain(CommonInput.EnvironmentNameOption));
            Assert.That(m_AccessModule.PlayerPolicyDeleteCommand!.Arguments, Does.Contain(PlayerPolicyInput.PlayerIdArgument));
            Assert.That(m_AccessModule.PlayerPolicyDeleteCommand!.Arguments, Does.Contain(AccessInput.StatementIdsArgument));
        });
    }

    [TestCase(typeof(IProjectPolicyApi))]
    [TestCase(typeof(IPlayerPolicyApi))]
    [TestCase(typeof(IAccessService))]
    public void AccessModuleModuleRegistersServices(Type serviceType)
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(
        [
            new AccessEndpoints()
        ]);
        var services = new List<ServiceDescriptor>();
        var hostBuilder = TestsHelper.CreateAndSetupMockHostBuilder(services);
        hostBuilder.ConfigureServices(AccessModule.RegisterServices);
        Assert.That(services.FirstOrDefault(c => c.ServiceType == serviceType), Is.Not.Null);
    }
}
