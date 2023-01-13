using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Cli.Common.Validator;

namespace Unity.Services.Cli.RemoteConfig.UnitTest;

[TestFixture]
class RemoteConfigModuleTests
{
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
}
