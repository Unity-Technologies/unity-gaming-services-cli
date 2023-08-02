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
using SortOrder = Unity.Services.Leaderboards.Authoring.Core.Model.SortOrder;
using TieringConfig = Unity.Services.Gateway.LeaderboardApiV1.Generated.Model.TieringConfig;
using UpdateType = Unity.Services.Leaderboards.Authoring.Core.Model.UpdateType;

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
        m_Leaderboard = new("lb1", "lb_name", SortOrder.Asc, UpdateType.Aggregate) { Path = k_Path };
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
        var mockRes = new UpdatedLeaderboardConfig(leaderboardId, "somename");
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
        var mockRes = new UpdatedLeaderboardConfig(leaderboardId, "somename")
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

    static Task<IEnumerable<UpdatedLeaderboardConfig>> ListFunc(
        string projectId,
        string envId,
        string? cursor,
        int? limit,
        CancellationToken token)
    {
        var remoteLbs = Enumerable.Range(0, 75)
            .Select(i => new UpdatedLeaderboardConfig($"id{i}", $"name{i}"));

        if (cursor == null)
        {
            return Task.FromResult(remoteLbs.Take(limit!.Value));
        }

        return Task.FromResult(remoteLbs.SkipWhile(l => l.Id != cursor).Skip(1).Take(limit!.Value));
    }
}
