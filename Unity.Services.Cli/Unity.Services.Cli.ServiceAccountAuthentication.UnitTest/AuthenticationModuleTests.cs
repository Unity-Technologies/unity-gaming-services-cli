using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Input;

namespace Unity.Services.Cli.Authentication.UnitTest;

[TestFixture]
class AuthenticationModuleTests
{
    [Test]
    public void GetCommandsForCliRootContainsExpectedCommands()
    {
        var authModule = new AuthenticationModule();

        var commandsForCliRoot = authModule.GetCommandsForCliRoot()
            .ToList();

        Assert.That(commandsForCliRoot, Contains.Item(authModule.LoginCommand));
        Assert.That(commandsForCliRoot, Contains.Item(authModule.LogoutCommand));
        Assert.That(commandsForCliRoot, Contains.Item(authModule.StatusCommand));
    }

    [Test]
    public void LoginCommandContainsExpectedInput()
    {
        var loginCommand = new AuthenticationModule().LoginCommand;

        CollectionAssert.Contains(loginCommand.Options, LoginInput.ServiceKeyIdOption);
        CollectionAssert.Contains(loginCommand.Options, LoginInput.SecretKeyOption);
    }

    [TestCase(typeof(IAuthenticator))]
    [TestCase(typeof(IServiceAccountAuthenticationService))]
    public void RegistersServicesRegisteredExpectedService(Type serviceType)
    {
        var collection = new ServiceCollection();
        collection.AddSingleton(new Mock<IConsolePrompt>().Object);
        var context = new HostBuilderContext(new Dictionary<object, object>());

        AuthenticationModule.RegisterServices(context, collection);

        Assert.That(collection.FirstOrDefault(c => c.ServiceType == serviceType), Is.Not.Null);
    }

    [Test]
    public void ModuleRootCommandIsNull()
    {
        ICommandModule module = new AuthenticationModule();

        Assert.That(module.ModuleRootCommand, Is.Null);
    }
}
