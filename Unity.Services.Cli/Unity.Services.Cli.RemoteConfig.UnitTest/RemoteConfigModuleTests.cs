using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.RemoteConfig.Templates;

namespace Unity.Services.Cli.RemoteConfig.UnitTest;

[TestFixture]
class RemoteConfigModuleTests
{
    Mock<RemoteConfigTemplate> m_MockTemplate = new Mock<RemoteConfigTemplate>();

    [TestCase(typeof(IRemoteConfigServicesWrapper))]
    public void ConfigureServicesRegistersExpectedServices(Type serviceType)
    {
        var services = new List<ServiceDescriptor>
        {
            ServiceDescriptor.Singleton(new Mock<IServiceAccountAuthenticationService>().Object),
            ServiceDescriptor.Singleton(new Mock<IConfigurationValidator>().Object),
            ServiceDescriptor.Singleton(new Mock<ILogger>().Object),
        };
        var hostBuilder = TestsHelper.CreateAndSetupMockHostBuilder(services);

        hostBuilder.ConfigureServices(RemoteConfigModule.RegisterServices);

        Assert.That(services.FirstOrDefault(c => c.ServiceType == serviceType), Is.Not.Null);
    }
    [Test]
    public void RemoteConfigModule_HasNewFileCommand()
    {
        var newFileCommand = new Command("test", "test")
            .AddNewFileCommand<RemoteConfigTemplate>("Remote Config");

        var module = new RemoteConfigModule();

        TestsHelper.AssertContainsCommand(module.ModuleRootCommand!, newFileCommand.Name, out var resultCommand);
        Assert.That(resultCommand, Is.EqualTo(newFileCommand));
    }
}
