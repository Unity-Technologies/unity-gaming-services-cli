using NUnit.Framework;
using Unity.Services.Cli.Leaderboards.Model;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.UnitTest.Model;

[TestFixture]
class LeaderboardResponseOutputTests
{
    UpdatedLeaderboardConfig m_UpdatedLeaderboardConfig = new (
        "",
        "",
        SortOrder.Asc,
        UpdateType.Aggregate,
        Decimal.One,
        new ResetConfig(new DateTime(2023, 1, 1), "@1d", true),
        new TieringConfig(TieringConfig.StrategyEnum.Percent, new List<TieringConfigTiersInner>(){new TieringConfigTiersInner("tier1", 2)}),
        DateTime.Today,
        DateTime.Today,
        DateTime.Now,
        new List<LeaderboardVersion>()
        );

    [SetUp]
    public void SetUp()
    {
        const string id = "id";
        const string name = "Test";
        const UpdateType updateType = UpdateType.Aggregate;
        const SortOrder sortOrder = SortOrder.Asc;
        const Decimal bucketSize = Decimal.One;
        var resetConfig = new ResetConfig(new DateTime(2023, 1, 1), "@1d", true);
        var tieringConfig = new TieringConfig(TieringConfig.StrategyEnum.Percent, new List<TieringConfigTiersInner>(){new TieringConfigTiersInner("tier1", 2)});
        var updated = new DateTime();
        var created = new DateTime();
        var lastReset = new DateTime();
        var versions = new List<LeaderboardVersion>();
        m_UpdatedLeaderboardConfig = new UpdatedLeaderboardConfig(id, name, sortOrder, updateType, bucketSize, resetConfig, tieringConfig, updated, created, lastReset, versions);
    }

    [Test]
    public void ConstructOutputScriptWithValidResponse()
    {
        var outputScript = m_UpdatedLeaderboardConfig;
        Assert.AreEqual(m_UpdatedLeaderboardConfig.Id, outputScript.Id);
        Assert.AreEqual(m_UpdatedLeaderboardConfig.Name, outputScript.Name);
        Assert.AreEqual(m_UpdatedLeaderboardConfig.SortOrder, outputScript.SortOrder);
        Assert.AreEqual(m_UpdatedLeaderboardConfig.UpdateType, outputScript.UpdateType);
        Assert.AreEqual(m_UpdatedLeaderboardConfig.ResetConfig, outputScript.ResetConfig);
        Assert.AreEqual(m_UpdatedLeaderboardConfig.TieringConfig, outputScript.TieringConfig);
        Assert.AreEqual(m_UpdatedLeaderboardConfig.Updated, outputScript.Updated);
        Assert.AreEqual(m_UpdatedLeaderboardConfig.Created, outputScript.Created);
        Assert.AreEqual(m_UpdatedLeaderboardConfig.LastReset, outputScript.LastReset);
        Assert.AreEqual(m_UpdatedLeaderboardConfig.Versions, outputScript.Versions);
    }

    [Test]
    public void OutputScriptToStringReturnFormattedString()
    {
        var outputScript = new GetLeaderboardResponseOutput(m_UpdatedLeaderboardConfig);
        var outputScriptString = outputScript.ToString();
        var expectedString = @"sortOrder: Asc
updateType: Aggregate
id: id
name: Test
bucketSize: 1
resetConfig:
  start: 2023-01-01T00:00:00.0000000
  schedule: '@1d'
  archive: true
tieringConfig:
  strategy: Percent
  tiers:
  - id: tier1
    cutoff: 2
updated: 0001-01-01T00:00:00.0000000
created: 0001-01-01T00:00:00.0000000
lastReset: 0001-01-01T00:00:00.0000000
versions: []
".Replace("\r\n", "\n")
            .Replace("\n", System.Environment.NewLine);
        Assert.AreEqual(expectedString, outputScriptString);
    }
}

