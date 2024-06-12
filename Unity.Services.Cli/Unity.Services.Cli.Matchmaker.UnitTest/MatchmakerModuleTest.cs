using System.CommandLine.Builder;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Matchmaker.Service;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Api;
using Unity.Services.Matchmaker.Authoring.Core.ConfigApi;
using Unity.Services.Matchmaker.Authoring.Core.Deploy;
using Unity.Services.Matchmaker.Authoring.Core.Fetch;
using Unity.Services.Matchmaker.Authoring.Core.IO;
using Unity.Services.Matchmaker.Authoring.Core.Parser;

namespace Unity.Services.Cli.Matchmaker.UnitTest;

[TestFixture]
public class MatchmakerModuleTest
{
    static readonly MatchmakerModule k_MatchmakerModule = new();

    [TestCase(typeof(IMatchmakerAdminApi))]
    [TestCase(typeof(IMatchmakerConfigParser))]
    [TestCase(typeof(IConfigApiClient))]
    [TestCase(typeof(IMatchmakerDeployHandler))]
    [TestCase(typeof(IMatchmakerFetchHandler))]
    [TestCase(typeof(IDeepEqualityComparer))]
    [TestCase(typeof(IMatchmakerService))]
    [TestCase(typeof(IFileSystem))]
    [TestCase(typeof(IDeploymentService))]
    [TestCase(typeof(IFetchService))]
    public void ConfigureMatchmakerRegistersExpectedServices(Type serviceType)
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(new[]
        {
            typeof(UnityServicesGatewayEndpoints).GetTypeInfo(),
            typeof(AdminApiTargetEndpoint).GetTypeInfo()
        });
        var services = new List<ServiceDescriptor>();
        var hostBuilder = TestsHelper.CreateAndSetupMockHostBuilder(services);
        hostBuilder.ConfigureServices(MatchmakerModule.RegisterServices);
        Assert.That(services.FirstOrDefault(c => c.ServiceType == serviceType), Is.Not.Null, $"Service {serviceType} not registered");
    }

    [Test]
    public void BuildCommands_CreateMatchmakerCommands()
    {
        var commandLineBuilder = new CommandLineBuilder();
        commandLineBuilder.AddModule(k_MatchmakerModule);
        TestsHelper.AssertContainsCommand(commandLineBuilder.Command, k_MatchmakerModule.ModuleRootCommand!.Name,
            out var resultCommand);

        Assert.That(resultCommand, Is.EqualTo(k_MatchmakerModule.ModuleRootCommand));
    }
}
