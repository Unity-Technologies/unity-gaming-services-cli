using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Service;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Api;

namespace Unity.Services.Cli.Leaderboards.UnitTest;

[TestFixture]
class LeaderboardModuleTests
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
        EndpointHelper.InitializeNetworkTargetEndpoints(
        [
            new LeaderboardEndpoints()
        ]);

        var collection = new ServiceCollection();
        collection.AddSingleton(ServiceDescriptor.Singleton(new Mock<ILeaderboardsApiAsync>().Object));
        collection.AddSingleton(ServiceDescriptor.Singleton(new Mock<IServiceAccountAuthenticationService>().Object));
        LeaderboardsModule.RegisterServices(new HostBuilderContext(new Dictionary<object, object>()), collection);
        Assert.That(collection.FirstOrDefault(c => c.ServiceType == serviceType), Is.Not.Null);
    }

    [Test]
    public void ScoresCommandExists()
    {
        var module = new LeaderboardsModule();
        TestsHelper.AssertContainsCommand(module.ModuleRootCommand, "scores", out var scoresCmd);
        TestsHelper.AssertContainsCommand(scoresCmd, "list", out _);
        TestsHelper.AssertContainsCommand(scoresCmd, "get", out _);
        TestsHelper.AssertContainsCommand(scoresCmd, "get-range", out _);
        TestsHelper.AssertContainsCommand(scoresCmd, "get-by-player-ids", out _);
        TestsHelper.AssertContainsCommand(scoresCmd, "delete", out _);
        TestsHelper.AssertContainsCommand(scoresCmd, "purge", out _);
    }

    [Test]
    public void ScoresListHasTierAndVersionOptions()
    {
        var module = new LeaderboardsModule();
        TestsHelper.AssertContainsCommand(module.ModuleRootCommand, "scores", out var scoresCmd);
        TestsHelper.AssertContainsCommand(scoresCmd, "list", out var listCmd);
        Assert.IsTrue(listCmd.Options.Contains(PaginatedLeaderboardInput.TierOption));
        Assert.IsTrue(listCmd.Options.Contains(LeaderboardIdInput.VersionOption));
    }

    [Test]
    public void ScoresGetHasVersionOption()
    {
        var module = new LeaderboardsModule();
        TestsHelper.AssertContainsCommand(module.ModuleRootCommand, "scores", out var scoresCmd);
        TestsHelper.AssertContainsCommand(scoresCmd, "get", out var getCmd);
        Assert.IsTrue(getCmd.Options.Contains(LeaderboardIdInput.VersionOption));
    }

    [Test]
    public void ScoresGetRangeHasVersionOption()
    {
        var module = new LeaderboardsModule();
        TestsHelper.AssertContainsCommand(module.ModuleRootCommand, "scores", out var scoresCmd);
        TestsHelper.AssertContainsCommand(scoresCmd, "get-range", out var getRangeCmd);
        Assert.IsTrue(getRangeCmd.Options.Contains(LeaderboardIdInput.VersionOption));
    }

    [Test]
    public void ScoresGetByPlayerIdsHasVersionOption()
    {
        var module = new LeaderboardsModule();
        TestsHelper.AssertContainsCommand(module.ModuleRootCommand, "scores", out var scoresCmd);
        TestsHelper.AssertContainsCommand(scoresCmd, "get-by-player-ids", out var getByIdsCmd);
        Assert.IsTrue(getByIdsCmd.Options.Contains(LeaderboardIdInput.VersionOption));
    }

    [Test]
    public void ScoresDeleteDoesNotHaveVersionOption()
    {
        var module = new LeaderboardsModule();
        TestsHelper.AssertContainsCommand(module.ModuleRootCommand, "scores", out var scoresCmd);
        TestsHelper.AssertContainsCommand(scoresCmd, "delete", out var deleteCmd);
        Assert.IsFalse(deleteCmd.Options.Contains(LeaderboardIdInput.VersionOption));
    }

    [Test]
    public void BucketsCommandExists()
    {
        var module = new LeaderboardsModule();
        TestsHelper.AssertContainsCommand(module.ModuleRootCommand, "buckets", out var bucketsCmd);
        TestsHelper.AssertContainsCommand(bucketsCmd, "list", out _);
        TestsHelper.AssertContainsCommand(bucketsCmd, "scores", out _);
    }

    [Test]
    public void BucketsListHasVersionButNotTierOption()
    {
        var module = new LeaderboardsModule();
        TestsHelper.AssertContainsCommand(module.ModuleRootCommand, "buckets", out var bucketsCmd);
        TestsHelper.AssertContainsCommand(bucketsCmd, "list", out var listCmd);
        Assert.IsTrue(listCmd.Options.Contains(LeaderboardIdInput.VersionOption));
        Assert.IsFalse(listCmd.Options.Contains(PaginatedLeaderboardInput.TierOption));
    }

    [Test]
    public void BucketsScoresHasTierAndVersionOptions()
    {
        var module = new LeaderboardsModule();
        TestsHelper.AssertContainsCommand(module.ModuleRootCommand, "buckets", out var bucketsCmd);
        TestsHelper.AssertContainsCommand(bucketsCmd, "scores", out var scoresCmd);
        Assert.IsTrue(scoresCmd.Options.Contains(PaginatedLeaderboardInput.TierOption));
        Assert.IsTrue(scoresCmd.Options.Contains(LeaderboardIdInput.VersionOption));
    }

    [Test]

    public void RetryAfterSleepDuration()
    {
        var response = new RestSharp.RestResponse();
        response.Headers = new List<RestSharp.HeaderParameter>()
        {
            new ("Retry-After", "1")
        };
        Polly.DelegateResult<RestSharp.RestResponse> res = new Polly.DelegateResult<RestSharp.RestResponse>(response);
        Assert.AreEqual(TimeSpan.FromSeconds(2), LeaderboardsModule.RetryAfterSleepDuration(2, res, new Polly.Context()));
    }
}
