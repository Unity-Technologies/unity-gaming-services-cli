using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Triggers.Deploy;
using Unity.Services.Triggers.Authoring.Core.Deploy;
using Unity.Services.Triggers.Authoring.Core.Fetch;
using Unity.Services.Triggers.Authoring.Core.IO;
using Unity.Services.Triggers.Authoring.Core.Model;
using Unity.Services.Triggers.Authoring.Core.Service;

namespace Unity.Services.Cli.Triggers.UnitTest.Deploy;

[TestFixture]
class TriggerFetchHandlerTests
{
    [Test]
    public async Task FetchAsync_CorrectResult()
    {
        var localTriggers = GetLocalConfigs();
        var remoteTriggers = GetRemoteConfigs();

        Mock<ITriggersClient> mockTriggersClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new TriggersFetchHandler(mockTriggersClient.Object, mockFileSystem.Object, new TriggersSerializer());

        mockTriggersClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteTriggers.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localTriggers
        );

        Assert.Contains(localTriggers.First(l => l.Id == "id1"), actualRes.Updated);
        Assert.Contains(localTriggers.First(l => l.Id == "id1"), actualRes.Fetched);
        Assert.Contains(localTriggers.First(l => l.Id == "id3"), actualRes.Deleted);
        Assert.Contains(localTriggers.First(l => l.Id == "id3"), actualRes.Fetched);
        Assert.IsEmpty(actualRes.Created);
    }

    [Test]
    public async Task FetchAsync_WriteCallsMade()
    {
        var localTriggers = GetLocalConfigs();
        var remoteTriggers = GetRemoteConfigs();

        Mock<ITriggersClient> mockTriggersClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new TriggersFetchHandler(mockTriggersClient.Object, mockFileSystem.Object, new TriggersSerializer());

        mockTriggersClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteTriggers.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localTriggers
        );

        mockFileSystem
            .Verify(f => f.WriteAllText(
                    "path1",
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

        mockFileSystem
            .Verify(f => f.WriteAllText(
                    "echo",
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);  //Should not happen unless reconcile
    }

    [Test]
    public async Task FetchAsync_DeleteCallsMade()
    {
        var localTriggers = GetLocalConfigs();
        var remoteTriggers = GetRemoteConfigs();

        Mock<ITriggersClient> mockTriggersClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new TriggersFetchHandler(mockTriggersClient.Object, mockFileSystem.Object, new TriggersSerializer());

        mockTriggersClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteTriggers.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localTriggers
        );

        mockFileSystem
            .Verify(f => f.Delete(
                    "path3",
                    It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Test]
    public async Task FetchAsync_WriteNewOnReconcile()
    {
        var localTriggers = GetLocalConfigs();
        var remoteTriggers = GetRemoteConfigs();

        Mock<ITriggersClient> mockTriggersClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new TriggersFetchHandler(mockTriggersClient.Object, mockFileSystem.Object, new TriggersSerializer());

        mockTriggersClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteTriggers.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localTriggers,
            reconcile: true
        );

        var expectedFile1 = new TriggersConfigFile(
            new List<TriggerConfig>() { (TriggerConfig)remoteTriggers[0] });
        mockFileSystem
            .Verify(f => f.WriteAllText(
                "path1",
                JsonConvert.SerializeObject(expectedFile1, Formatting.Indented),
                It.IsAny<CancellationToken>()),
                Times.Once);
        var expectedFile2 = new TriggersConfigFile(
            new List<TriggerConfig>() { (TriggerConfig)remoteTriggers[1] });
        mockFileSystem
            .Verify(f => f.WriteAllText(
                Path.Combine("dir", "name2.tr"),
                JsonConvert.SerializeObject(expectedFile2, Formatting.Indented),
                It.IsAny<CancellationToken>()),
                Times.Once);
        mockFileSystem.Verify(f => f.Delete("path3", It.IsAny<CancellationToken>()));
    }

    [Test]
    public async Task FetchAsync_DryRunNoCalls()
    {
        var localTriggers = GetLocalConfigs();
        var remoteTriggers = GetRemoteConfigs();

        Mock<ITriggersClient> mockTriggersClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new TriggersFetchHandler(mockTriggersClient.Object, mockFileSystem.Object, new TriggersSerializer());

        mockTriggersClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteTriggers.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localTriggers,
            dryRun: true
        );

        mockFileSystem
            .Verify(f => f.Delete(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

        mockFileSystem
            .Verify(f => f.WriteAllText(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Test]
    public async Task FetchAsync_DuplicateNameNotDeleted()
    {
        var localTriggers = GetLocalConfigs();
        var triggerConfig = new TriggerConfig("otherId", "name1", "changedEventType", "cloud-code", "actionUrn", "") { Path = "" };

        localTriggers.Add(triggerConfig);
        var remoteTriggers = GetRemoteConfigs();

        Mock<ITriggersClient> mockTriggersClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new TriggersFetchHandler(mockTriggersClient.Object, mockFileSystem.Object, new TriggersSerializer());

        mockTriggersClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteTriggers.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localTriggers,
            dryRun: true
        );

        mockFileSystem
            .Verify(f => f.Delete(
                    It.Is<string>(s => s == "path1"),
                    It.IsAny<CancellationToken>()),
                Times.Never);

        Assert.Contains(localTriggers.FirstOrDefault(l => l.Id == "otherId"), actualRes.Failed);
        Assert.Contains(localTriggers.FirstOrDefault(l => l.Id == "id1"), actualRes.Failed);
    }

    static List<ITriggerConfig> GetLocalConfigs()
    {
        var triggers = new List<ITriggerConfig>()
        {
            new TriggerConfig("id1", "name1", "eventType", "cloud-code", "actionUrn", "")
            {
                Path = "path1"
            },
            new TriggerConfig("id3", "name3", "eventType", "cloud-code", "actionUrn", "")
            {
                Path = "path3"
            }
        };
        return triggers;
    }

    static List<ITriggerConfig> GetRemoteConfigs()
    {
        var triggers = new List<ITriggerConfig>()
        {
            new TriggerConfig("id1", "name1", "eventType", "cloud-code", "actionUrn", "")
            {
                Path = "Remote"
            },
            new TriggerConfig("id2", "name2", "eventType", "cloud-code", "actionUrn", "")
            {
                Path = "Remote"
            },
        };
        return triggers;
    }
}
