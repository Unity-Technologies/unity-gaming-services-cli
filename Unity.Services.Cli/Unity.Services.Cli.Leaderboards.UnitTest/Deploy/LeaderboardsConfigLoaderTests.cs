using NUnit.Framework;
using Moq;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Deploy;
using Unity.Services.Leaderboards.Authoring.Core.Deploy;
using Unity.Services.Leaderboards.Authoring.Core.Fetch;
using Unity.Services.Leaderboards.Authoring.Core.IO;
using Unity.Services.Leaderboards.Authoring.Core.Model;
using Unity.Services.Leaderboards.Authoring.Core.Service;

namespace Unity.Services.Cli.Leaderboards.UnitTest.Deploy;

[TestFixture]
public class LeaderboardsConfigLoaderTests
{
    LeaderboardsConfigLoader? m_LeaderboardsConfigLoader;
    readonly Mock<IFileSystem> m_FileSystem = new();

    [Test]
    public async Task ConfigLoader_Deserializes()
    {
        m_LeaderboardsConfigLoader = new LeaderboardsConfigLoader(
            m_FileSystem.Object);
        var content = @"{
  'SortOrder': 'asc',
  'UpdateType': 'keepBest',
  'Name': 'My Complex LB',
  'BucketSize': 20.0,
  'ResetConfig': {
    'Start': '2023-07-01T16:00:00Z',
    'Schedule': '0 12 1 * *'
  },
  'TieringConfig': {
    'Strategy': 'score',
    'Tiers': [
      {
        'Id': 'Gold',
        'Cutoff': 200.0
      },
      {
        'Id': 'Silver',
        'Cutoff': 150.0
      },
      {
        'Id': 'Bronze'
      }
    ]
  }
}";
        m_FileSystem.Setup(f => f.ReadAllText(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        var configs = await m_LeaderboardsConfigLoader
            .LoadConfigsAsync(new[] { "path" }, CancellationToken.None);

        Assert.AreEqual(1, configs.Loaded.Count);
        Assert.AreEqual(0, configs.Failed.Count);
        var config = configs.Loaded.First();

        Assert.AreEqual("path", config.Id);
        Assert.AreEqual(SortOrder.Asc, config.SortOrder);
        Assert.AreEqual(UpdateType.KeepBest, config.UpdateType);
        Assert.AreEqual(20.0, config.BucketSize);
        Assert.AreEqual(3, config.TieringConfig.Tiers.Count);
        Assert.AreEqual("Gold", config.TieringConfig.Tiers[0].Id);
        Assert.AreEqual("Silver", config.TieringConfig.Tiers[1].Id);
        Assert.AreEqual("Bronze", config.TieringConfig.Tiers[2].Id);
    }

    [Test]
    public async Task ConfigLoader_ReportsFailures()
    {
        m_LeaderboardsConfigLoader = new LeaderboardsConfigLoader(
            m_FileSystem.Object);
        var content = @"{
  'SortOrder': 'asc',
  'UpdateType': 'keepBest',
  'Name': 'My Complex LB',
  'BucketSize': 'hi'";
        m_FileSystem.Setup(f => f.ReadAllText(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        var configs = await m_LeaderboardsConfigLoader
            .LoadConfigsAsync(new[] { "path" }, CancellationToken.None);

        Assert.AreEqual(0, configs.Loaded.Count);
        Assert.AreEqual(1, configs.Failed.Count);
    }
}
