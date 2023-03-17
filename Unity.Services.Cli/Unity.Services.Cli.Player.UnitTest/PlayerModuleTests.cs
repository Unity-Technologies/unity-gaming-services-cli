using System.CommandLine.Builder;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Player.Input;
using Unity.Services.Cli.Player.Networking;
using Unity.Services.Cli.Player.Service;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Player.UnitTest;

[TestFixture]
public class PlayerModuleTests
{
    static readonly PlayerModule k_PlayerModule = new ();

    [Test]
    public void BuildCommands_CreateCommands()
    {
        var commandLineBuilder = new CommandLineBuilder();
        commandLineBuilder.AddModule(k_PlayerModule);
        TestsHelper.AssertContainsCommand(commandLineBuilder.Command, k_PlayerModule.ModuleRootCommand!.Name,
            out var resultCommand);
        Assert.Multiple(() =>
        {
            Assert.That(resultCommand, Is.EqualTo(k_PlayerModule.ModuleRootCommand));
            Assert.That(k_PlayerModule.DeleteCommand!.Handler, Is.Not.Null);
            Assert.That(k_PlayerModule.CreateCommand!.Handler, Is.Not.Null);
            Assert.That(k_PlayerModule.EnableCommand!.Handler, Is.Not.Null);
            Assert.That(k_PlayerModule.DisableCommand!.Handler, Is.Not.Null);
        });
    }

    [Test]
    public void BuildCommands_ContainsRequiredInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(k_PlayerModule.DeleteCommand!.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(k_PlayerModule.DeleteCommand.Arguments, Does.Contain(PlayerInput.PlayerIdArgument));
            Assert.That(k_PlayerModule.CreateCommand!.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(k_PlayerModule.EnableCommand!.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(k_PlayerModule.EnableCommand.Arguments, Does.Contain(PlayerInput.PlayerIdArgument));
            Assert.That(k_PlayerModule.DisableCommand!.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(k_PlayerModule.DisableCommand.Arguments, Does.Contain(PlayerInput.PlayerIdArgument));
        });
    }

    [Test]
    public void RegisterServices_ExpectedServices()
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(new[]
        {
            typeof(PlayerAdminEndpoints).GetTypeInfo(),
            typeof(PlayerAuthEndpoints).GetTypeInfo(),
        });

        var services = new List<ServiceDescriptor>
        {
            ServiceDescriptor.Singleton(new Mock<IServiceAccountAuthenticationService>().Object)
        };
        var hostBuilder = TestsHelper.CreateAndSetupMockHostBuilder(services);
        hostBuilder.ConfigureServices(PlayerModule.RegisterServices);
        TestsHelper.AssertHasServiceType<IPlayerService>(services);
    }
}
