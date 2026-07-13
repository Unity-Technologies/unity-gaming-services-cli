using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Observability.Input;
using Unity.Services.Cli.Observability.Service;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.ObservabilityApiV1.Generated.Api;

namespace Unity.Services.Cli.Observability.UnitTest;

[TestFixture]
class ObservabilityModuleTests
{
    [Test]
    public void LogsListCommandExists()
    {
        var module = new ObservabilityModule();
        TestsHelper.AssertContainsCommand(module.ModuleRootCommand, "logs", out var logsCmd);
        TestsHelper.AssertContainsCommand(logsCmd, "list", out _);
    }

    [Test]
    public void RootCommandHasObsAlias()
    {
        var module = new ObservabilityModule();
        Assert.IsTrue(module.ModuleRootCommand.Aliases.Contains("obs"));
    }

    [Test]
    public void ListCommandWithInput()
    {
        var module = new ObservabilityModule();

        Assert.IsTrue(module.ListLogsCommand.Options.Contains(CommonInput.CloudProjectIdOption));
        Assert.IsTrue(module.ListLogsCommand.Options.Contains(CommonInput.EnvironmentNameOption));
        Assert.IsTrue(module.ListLogsCommand.Options.Contains(ListLogsInput.FromOption));
        Assert.IsTrue(module.ListLogsCommand.Options.Contains(ListLogsInput.ToOption));
        Assert.IsTrue(module.ListLogsCommand.Options.Contains(ListLogsInput.QueryOption));
        Assert.IsTrue(module.ListLogsCommand.Options.Contains(ListLogsInput.OffsetOption));
        Assert.IsTrue(module.ListLogsCommand.Options.Contains(ListLogsInput.LimitOption));
    }

    [TestCase(typeof(IObservabilityService))]
    public void ConfigureObservabilityRegistersExpectedServices(Type serviceType)
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(
        [
            new ObservabilityEndpoints()
        ]);

        var collection = new ServiceCollection();
        collection.AddSingleton(ServiceDescriptor.Singleton(new Mock<ILogsApiAsync>().Object));
        collection.AddSingleton(ServiceDescriptor.Singleton(new Mock<IServiceAccountAuthenticationService>().Object));
        ObservabilityModule.RegisterServices(new HostBuilderContext(new Dictionary<object, object>()), collection);
        Assert.That(collection.FirstOrDefault(c => c.ServiceType == serviceType), Is.Not.Null);
    }
}
