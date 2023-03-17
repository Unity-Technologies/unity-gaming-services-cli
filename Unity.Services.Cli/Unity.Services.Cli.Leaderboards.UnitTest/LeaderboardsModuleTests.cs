using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Service;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Api;

namespace Unity.Services.Cli.Leaderboards.UnitTest;

[TestFixture]
internal class LeaderboardModuleTests
{
    [Test]
    public void ListCommandWithInput()
    {
        LeaderboardsModule module = new();

        Assert.IsTrue(module.ListLeaderboardsCommand.Options.Contains(CommonInput.CloudProjectIdOption));
        Assert.IsTrue(module.ListLeaderboardsCommand.Options.Contains(CommonInput.EnvironmentNameOption));
        Assert.IsTrue(module.ListLeaderboardsCommand.Options.Contains(ListLeaderboardInput.CursorOption));
        Assert.IsTrue(module.ListLeaderboardsCommand.Options.Contains(ListLeaderboardInput.LimitOption));
    }

    [TestCase(typeof(ILeaderboardsService))]
    public void ConfigureLeaderboardRegistersExpectedServices(Type serviceType)
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(new[]
        {
            typeof(LeaderboardEndpoints).GetTypeInfo()
        });

        var collection = new ServiceCollection();
        collection.AddSingleton(ServiceDescriptor.Singleton(new Mock<ILeaderboardsApiAsync>().Object));
        collection.AddSingleton(ServiceDescriptor.Singleton(new Mock<IServiceAccountAuthenticationService>().Object));
        LeaderboardsModule.RegisterServices(new HostBuilderContext(new Dictionary<object, object>()), collection);
        Assert.That(collection.FirstOrDefault(c => c.ServiceType == serviceType), Is.Not.Null);
    }
}
