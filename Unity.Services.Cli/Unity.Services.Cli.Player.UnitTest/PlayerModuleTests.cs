using System.CommandLine.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Player.Input;
using Unity.Services.Cli.Player.Networking;
using Unity.Services.Cli.Player.Service;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.Player.IdProvider.Service;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Player.UnitTest;

[TestFixture]
public class PlayerModuleTests
{
    static readonly PlayerModule k_PlayerModule = new();

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
            Assert.That(k_PlayerModule.GetCommand!.Handler, Is.Not.Null);
            Assert.That(k_PlayerModule.ListCommand!.Handler, Is.Not.Null);
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
            Assert.That(k_PlayerModule.GetCommand!.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(k_PlayerModule.GetCommand.Arguments, Does.Contain(PlayerInput.PlayerIdArgument));
            Assert.That(k_PlayerModule.ListCommand!.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(k_PlayerModule.ListCommand.Options, Does.Contain(PlayerInput.PlayersLimitOption));
            Assert.That(k_PlayerModule.ListCommand.Options, Does.Contain(PlayerInput.PlayersPageOption));
        });
    }

    static ISystemEnvironmentProvider Preview(string? value)
    {
        var environment = new Mock<ISystemEnvironmentProvider>();
        var error = "";
        environment.Setup(e => e.GetSystemEnvironmentVariable(Keys.EnvironmentKeys.Preview, out error)).Returns(value);
        return environment.Object;
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("some-other-preview")]
    public void IdProviderCommands_AreOffUnlessTheirPreviewIsEnabled(string? preview)
    {
        var module = new PlayerModule(Preview(preview));

        Assert.That(module.IdProviderCommand, Is.Null);
        Assert.That(module.ModuleRootCommand!.Subcommands.Select(c => c.Name), Does.Not.Contain("id-provider"));
    }

    [Test]
    public void IdProviderCommands_AreAddedWhenTheirPreviewIsEnabled()
    {
        var module = new PlayerModule(Preview(PreviewFeatures.PlayerIdProviders));

        Assert.That(module.ModuleRootCommand!.Subcommands, Does.Contain(module.IdProviderCommand));
    }

    [Test]
    public void RegisterServices_ExpectedServices()
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(
        [
            new PlayerAdminEndpoints(),
            new PlayerAuthEndpoints(),
            new PlayerIdentityEndpoints(),
        ]);

        var services = new List<ServiceDescriptor>
        {
            ServiceDescriptor.Singleton(new Mock<IServiceAccountAuthenticationService>().Object)
        };
        var hostBuilder = TestsHelper.CreateAndSetupMockHostBuilder(services);
        hostBuilder.ConfigureServices(PlayerModule.RegisterServices);
        TestsHelper.AssertHasServiceType<IPlayerService>(services);
        TestsHelper.AssertHasServiceType<IIdProviderService>(services);
    }
}
