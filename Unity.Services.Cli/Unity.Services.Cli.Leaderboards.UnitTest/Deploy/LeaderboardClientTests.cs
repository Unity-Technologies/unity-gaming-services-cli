using System.Net;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Leaderboards.Deploy;
using Unity.Services.Cli.Leaderboards.Service;
using Unity.Services.Cli.Leaderboards.UnitTest.Utils;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Client;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;
using Unity.Services.Leaderboards.Authoring.Core.Model;
using LeaderboardConfig = Unity.Services.Leaderboards.Authoring.Core.Model.LeaderboardConfig;
using ResetConfig = Unity.Services.Gateway.LeaderboardApiV1.Generated.Model.ResetConfig;
using CoreSortOrder = Unity.Services.Leaderboards.Authoring.Core.Model.SortOrder;
using TieringConfig = Unity.Services.Gateway.LeaderboardApiV1.Generated.Model.TieringConfig;
using CoreUpdateType = Unity.Services.Leaderboards.Authoring.Core.Model.UpdateType;
using SortOrder = Unity.Services.Gateway.LeaderboardApiV1.Generated.Model.SortOrder;
using UpdateType = Unity.Services.Gateway.LeaderboardApiV1.Generated.Model.UpdateType;

namespace Unity.Services.Cli.Leaderboards.UnitTest.Deploy;

[TestFixture]
public class LeaderboardClientTests
{
    const string k_Name = "path1.lb";
    const string k_Path = "foo/path1.lb";
    const string k_Content = "{ id: \"lb1\", name: \"lb_name\", path: \"foo/path1.lb\" }";
    readonly LeaderboardConfig m_Leaderboard;

    public LeaderboardClientTests()
    {
        m_Leaderboard = new("lb1", "lb_name", CoreSortOrder.Asc, CoreUpdateType.Aggregate) { Path = k_Path };
    }

    [Test]
    public void Initialize_Succeed()
    {
        Mock<ILeaderboardsService> service = new();
        var client = new LeaderboardsClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        Assert.AreEqual(client.EnvironmentId, TestValues.ValidEnvironmentId);
        Assert.AreEqual(client.ProjectId, TestValues.ValidProjectId);
        Assert.AreEqual(client.CancellationToken, CancellationToken.None);
    }

    [Test]
    public async Task ListMoreThanLimit()
    {
        Mock<ILeaderboardsService> service = new();
        var client = new LeaderboardsClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);

        service.Setup(
            s => s.GetLeaderboardsAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()
            ))
            .Returns(ListFunc);

        var list = await client.List(CancellationToken.None);
        service.Verify(s => s.GetLeaderboardsAsync(It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        Assert.AreEqual(75, list.Count);
    }

    [Test]
    public async Task ListWhenThereAreNone()
    {
        Mock<ILeaderboardsService> service = new();
        var client = new LeaderboardsClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);

        service.Setup(
                s => s.GetLeaderboardsAsync(
                    TestValues.ValidProjectId,
                    TestValues.ValidEnvironmentId,
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                ))
            .Returns(Task.FromResult((IEnumerable<UpdatedLeaderboardConfig>)Array.Empty<UpdatedLeaderboardConfig>()));

        var list = await client.List(CancellationToken.None);
        Assert.AreEqual(0, list.Count);
    }

    [Test]
    public async Task UploadMapsToUpload()
    {
        Mock<ILeaderboardsService> service = new();
        var client = new LeaderboardsClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        await client.Update(m_Leaderboard!, CancellationToken.None);

        service
            .Verify(
                s => s.UpdateLeaderboardAsync(
                    TestValues.ValidProjectId,
                    TestValues.ValidEnvironmentId,
                    m_Leaderboard.Id,
                It.Is<LeaderboardPatchConfig>(l => l.Name == m_Leaderboard.Name),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
    }

    [Test]
    public void UpdateExceptionPropagates()
    {
        Mock<ILeaderboardsService> service = new();
        var client = new LeaderboardsClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        var exceptionMsg = "unknown exception";
        service.Setup(x => x.UpdateLeaderboardAsync(TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId, "lb1", It.IsAny<LeaderboardPatchConfig>(), CancellationToken.None))
            .ThrowsAsync(new Exception(exceptionMsg));

        Assert.ThrowsAsync<Exception>( async () => await client.Update(m_Leaderboard!, CancellationToken.None) );
    }

    [Test]
    public async Task CreateMapsToCreate()
    {
        Mock<ILeaderboardsService> service = new();
        var client = new LeaderboardsClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        await client.Create(m_Leaderboard!, CancellationToken.None);

        service
            .Verify(
                s => s.CreateLeaderboardAsync(
                    TestValues.ValidProjectId,
                    TestValues.ValidEnvironmentId,
                    It.Is<LeaderboardIdConfig>(l => l.Id == m_Leaderboard.Id && l.Name == m_Leaderboard.Name),
                    It.IsAny<CancellationToken>()),
                Times.Once());
    }

    [Test]
    public async Task DeleteMapsToDelete()
    {
        Mock<ILeaderboardsService> serviceMock = new();
        var client = new LeaderboardsClient(serviceMock.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        await client.Delete(m_Leaderboard!, CancellationToken.None);

        serviceMock
            .Verify(
                s => s.DeleteLeaderboardAsync(
                    TestValues.ValidProjectId,
                    TestValues.ValidEnvironmentId,
                    m_Leaderboard.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once());
    }

    [Test]
    public async Task GetMapsToGet()
    {
        Mock<ILeaderboardsService> service = new();
        var client = new LeaderboardsClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        var leaderboardId = "someid";
        var mockRes = new UpdatedLeaderboardConfig(leaderboardId, "somename", SortOrder.Asc, UpdateType.Aggregate);
        service.Setup(
            s => s.GetLeaderboardAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                leaderboardId,
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ApiResponse<UpdatedLeaderboardConfig>(HttpStatusCode.Accepted, mockRes)));

        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        var res = await client.Get(leaderboardId, CancellationToken.None);

        service
            .Verify(
                s => s.GetLeaderboardAsync(
                    TestValues.ValidProjectId,
                    TestValues.ValidEnvironmentId,
                    leaderboardId,
                    It.IsAny<CancellationToken>()),
                Times.Once());

        Assert.AreEqual(mockRes.Id, res.Id);
        Assert.AreEqual(mockRes.Name, res.Name);
    }

    [Test]
    public async Task GetMapsComplexStructure()
    {
        Mock<ILeaderboardsService> service = new();
        var client = new LeaderboardsClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        var leaderboardId = "someid";
        var mockRes = new UpdatedLeaderboardConfig(leaderboardId, "somename", SortOrder.Asc, UpdateType.Aggregate)
        {
            ResetConfig = new ResetConfig(),
            TieringConfig = new TieringConfig(TieringConfig.StrategyEnum.Score,new List<TieringConfigTiersInner>()
            {
                new ("gold")
            })
        };
        service.Setup(
                s => s.GetLeaderboardAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    leaderboardId,
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ApiResponse<UpdatedLeaderboardConfig>(HttpStatusCode.Accepted, mockRes)));

        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        var res = await client.Get(leaderboardId, CancellationToken.None);

        service
            .Verify(
                s => s.GetLeaderboardAsync(
                    TestValues.ValidProjectId,
                    TestValues.ValidEnvironmentId,
                    leaderboardId,
                    It.IsAny<CancellationToken>()),
                Times.Once());

        Assert.AreEqual(mockRes.Id, res.Id);
        Assert.AreEqual(mockRes.Name, res.Name);
        Assert.AreEqual((int)mockRes.TieringConfig.Strategy, (int)res.TieringConfig.Strategy);
        Assert.AreEqual(mockRes.TieringConfig.Tiers.First().Id, res.TieringConfig.Tiers.First().Id);
        Assert.AreEqual(mockRes.ResetConfig.Archive, res.ResetConfig.Archive);
        Assert.AreEqual(mockRes.ResetConfig.Schedule, res.ResetConfig.Schedule);
        Assert.AreEqual(mockRes.ResetConfig.Start, res.ResetConfig.Start);
    }

    [Test]
    [TestCase(SortOrder.Asc, UpdateType.Aggregate)]
    [TestCase(SortOrder.Desc, UpdateType.KeepBest)]
    [TestCase(SortOrder.Desc, UpdateType.KeepLatest)]
    public async Task GetMapsSortAndUpdate(SortOrder order, UpdateType updateType)
    {
        Mock<ILeaderboardsService> service = new();
        var client = new LeaderboardsClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        var leaderboardId = "someid";
        var mockRes = new UpdatedLeaderboardConfig(leaderboardId, "somename")
        {
            ResetConfig = new ResetConfig(),
            SortOrder = order,
            UpdateType = updateType
        };
        service.Setup(
                s => s.GetLeaderboardAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    leaderboardId,
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ApiResponse<UpdatedLeaderboardConfig>(HttpStatusCode.Accepted, mockRes)));

        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        var res = await client.Get(leaderboardId, CancellationToken.None);

        service
            .Verify(
                s => s.GetLeaderboardAsync(
                    TestValues.ValidProjectId,
                    TestValues.ValidEnvironmentId,
                    leaderboardId,
                    It.IsAny<CancellationToken>()),
                Times.Once());

        Assert.AreEqual(mockRes.Id, res.Id);
        Assert.AreEqual(mockRes.Name, res.Name);
        Assert.AreEqual(order == SortOrder.Asc ? CoreSortOrder.Asc : CoreSortOrder.Desc, res.SortOrder);
        Assert.AreEqual(
            updateType == UpdateType.Aggregate
                ? CoreUpdateType.Aggregate
                : (updateType == UpdateType.KeepBest
                    ? CoreUpdateType.KeepBest : CoreUpdateType.KeepLatest),
            res.UpdateType);
    }

    [Test]
    public void GetThrowsOnInvalidSort()
    {
        Mock<ILeaderboardsService> service = new();
        var client = new LeaderboardsClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        var leaderboardId = "someid";
        var mockRes = new UpdatedLeaderboardConfig(leaderboardId, "somename")
        {
            SortOrder = 0,
        };
        service.Setup(
                s => s.GetLeaderboardAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    leaderboardId,
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ApiResponse<UpdatedLeaderboardConfig>(HttpStatusCode.Accepted, mockRes)));

        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await client.Get(leaderboardId, CancellationToken.None));
    }

    [Test]
    public void GetThrowsOnInvalidUpdate()
    {
        Mock<ILeaderboardsService> service = new();
        var client = new LeaderboardsClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        var leaderboardId = "someid";
        var mockRes = new UpdatedLeaderboardConfig(leaderboardId, "somename")
        {
            UpdateType = 0,
        };
        service.Setup(
                s => s.GetLeaderboardAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    leaderboardId,
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ApiResponse<UpdatedLeaderboardConfig>(HttpStatusCode.Accepted, mockRes)));

        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await client.Get(leaderboardId, CancellationToken.None));
    }

    [Test]
    [TestCase((int)CoreSortOrder.Asc, (int)CoreUpdateType.Aggregate)]
    [TestCase((int)CoreSortOrder.Desc, (int)CoreUpdateType.KeepBest)]
    [TestCase((int)CoreSortOrder.Desc, (int)CoreUpdateType.KeepLatest)]
    public async Task UpdateMapsSortAndUpdate(int sortInt, int updateInt)
    {
        var sort = (CoreSortOrder)sortInt;
        var update = (CoreUpdateType)updateInt;
        Mock<ILeaderboardsService> service = new();
        var client = new LeaderboardsClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        LeaderboardConfig config = new("lb1", "lb_name", sort, update) { Path = k_Path };
        await client.Update(config, CancellationToken.None);

        var expectedSort = sort == CoreSortOrder.Asc ? SortOrder.Asc : SortOrder.Desc;
        var expectedUpdate = update == CoreUpdateType.Aggregate
            ? UpdateType.Aggregate
            : (update == CoreUpdateType.KeepBest ? UpdateType.KeepBest : UpdateType.KeepLatest);

        service
            .Verify(
                s => s.UpdateLeaderboardAsync(
                    TestValues.ValidProjectId,
                    TestValues.ValidEnvironmentId,
                    m_Leaderboard.Id,
                    It.Is<LeaderboardPatchConfig>(l =>
                        l.Name == m_Leaderboard.Name
                        && l.SortOrder == expectedSort
                        && l.UpdateType == expectedUpdate),
                    It.IsAny<CancellationToken>()),
                Times.Once());
    }

    [Test]
    public async Task UpdateTiers()
    {
        Mock<ILeaderboardsService> service = new();
        var client = new LeaderboardsClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        LeaderboardConfig config = new("lb1", "lb_name")
        {
            Path = k_Path,
            TieringConfig = new Services.Leaderboards.Authoring.Core.Model.TieringConfig()
            {
                Strategy = Strategy.Score,
                Tiers = new List<Tier>
                {
                    new () { Id = "Tier1", Cutoff = 10.0 },
                    new () { Id = "Tier2", Cutoff = 15.0 },
                }
            }
        };
        await client.Update(config, CancellationToken.None);

        service
            .Verify(
                s => s.UpdateLeaderboardAsync(
                    TestValues.ValidProjectId,
                    TestValues.ValidEnvironmentId,
                    m_Leaderboard.Id,
                    It.Is<LeaderboardPatchConfig>(l =>
                        l.Name == m_Leaderboard.Name
                        && l.TieringConfig.Tiers.Count == 2
                        && l.TieringConfig.Strategy == TieringConfig.StrategyEnum.Score
                        && l.TieringConfig.Tiers[0].Id == "Tier1"),
                    It.IsAny<CancellationToken>()),
                Times.Once());
    }

    [Test]
    public async Task UpdateResetConfig()
    {
        Mock<ILeaderboardsService> service = new();
        var client = new LeaderboardsClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        var today = DateTime.Today;
        var schedule = "foo";
        LeaderboardConfig config = new("lb1", "lb_name")
        {
            Path = k_Path,
            ResetConfig = new Services.Leaderboards.Authoring.Core.Model.ResetConfig()
            {
                Archive = true,
                Schedule = schedule,
                Start = today
            }
        };
        await client.Update(config, CancellationToken.None);

        service
            .Verify(
                s => s.UpdateLeaderboardAsync(
                    TestValues.ValidProjectId,
                    TestValues.ValidEnvironmentId,
                    m_Leaderboard.Id,
                    It.Is<LeaderboardPatchConfig>(l =>
                        l.Name == m_Leaderboard.Name
                        && l.ResetConfig.Archive
                        && l.ResetConfig.Schedule == schedule
                        && l.ResetConfig.Start == today),
                    It.IsAny<CancellationToken>()),
                Times.Once());
    }

    [Test]
    public void UpdateThrowsOnInvalidSort()
    {
        Mock<ILeaderboardsService> service = new();
        var client = new LeaderboardsClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        LeaderboardConfig config = new("lb1", "lb_name")
        {
            Path = k_Path,
            SortOrder = 0
        };
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await client.Update(config, CancellationToken.None));
    }

    [Test]
    public void UpdateThrowsOnInvalidUpdate()
    {
        Mock<ILeaderboardsService> service = new();
        var client = new LeaderboardsClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        LeaderboardConfig config = new("lb1", "lb_name")
        {
            Path = k_Path,
            UpdateType = 0
        };
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await client.Update(config, CancellationToken.None));
    }


    static Task<IEnumerable<UpdatedLeaderboardConfig>> ListFunc(
        string projectId,
        string envId,
        string? cursor,
        int? limit,
        CancellationToken token)
    {
        var remoteLbs = Enumerable.Range(0, 75)
            .Select(i => new UpdatedLeaderboardConfig($"id{i}", $"name{i}", SortOrder.Asc, UpdateType.Aggregate));

        if (cursor == null)
        {
            return Task.FromResult(remoteLbs.Take(limit!.Value));
        }

        return Task.FromResult(remoteLbs.SkipWhile(l => l.Id != cursor).Skip(1).Take(limit!.Value));
    }
}
