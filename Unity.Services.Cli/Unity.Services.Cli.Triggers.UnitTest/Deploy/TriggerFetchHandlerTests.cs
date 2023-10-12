using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Triggers.Deploy;
using Unity.Services.Triggers.Authoring.Core.Deploy;
using Unity.Services.Triggers.Authoring.Core.Fetch;
using Unity.Services.Triggers.Authoring.Core.IO;
using Unity.Services.Triggers.Authoring.Core.Model;
using Unity.Services.Triggers.Authoring.Core.Service;

namespace Unity.Services.Cli.Triggers.UnitTest.Deploy;

[TestFixture]
[Ignore("Fetch not in scope")]
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

        var expectedJson = "{\"Configs\":[{\"Id\":\"id1\",\"Name\":\"name1\",\"EventType\":\"eventType\",\"ActionType\":\"cloud-code\",\"ActionUrn\":\"actionUrn\"}]}";
        mockFileSystem
            .Verify(f => f.WriteAllText(
                "path1",
                expectedJson,
                It.IsAny<CancellationToken>()),
                Times.Once);
        var expectedJson2 = "{\"Configs\":[{\"Id\":\"id2\",\"Name\":\"name2\",\"EventType\":\"eventType\",\"ActionType\":\"cloud-code\",\"ActionUrn\":\"actionUrn\"}]}";
        mockFileSystem
            .Verify(f => f.WriteAllText(
                Path.Combine("dir", "name2.tr"),
                expectedJson2,
                It.IsAny<CancellationToken>()),
                Times.Once);
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
    public async Task FetchAsync_DuplicateIdNotDeleted()
    {
        var localTriggers = GetLocalConfigs();
        var triggerConfig = new TriggerConfig("id1", "changed name", "eventType", "cloud-code", "actionUrn") { Path = ""};

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

        Assert.Contains(localTriggers.FirstOrDefault(l => l.Name == "changed name"), actualRes.Failed);
        Assert.Contains(localTriggers.FirstOrDefault(l => l.Name == "name1"), actualRes.Failed);
    }

    static List<ITriggerConfig> GetLocalConfigs()
    {
        var triggers = new List<ITriggerConfig>()
        {
            new TriggerConfig("id1", "name1", "eventType", "cloud-code", "actionUrn")
            {
                Path = "path1"
            },
            new TriggerConfig("id3", "name3", "eventType", "cloud-code", "actionUrn")
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
            new TriggerConfig("id1", "name1", "eventType", "cloud-code", "actionUrn")
            {
                Path = "Remote"
            },
            new TriggerConfig("id2", "name2", "eventType", "cloud-code", "actionUrn")
            {
                Path = "Remote"
            },
        };
        return triggers;
    }
}
