using Moq;
using NUnit.Framework;
using Unity.Services.Triggers.Authoring.Core.Deploy;
using Unity.Services.Triggers.Authoring.Core.Model;
using Unity.Services.Triggers.Authoring.Core.Service;

namespace Unity.Services.Cli.Triggers.UnitTest.Deploy;

[TestFixture]
class TriggerDeploymentHandlerTests
{
    [Test]
    public async Task DeployAsync_CorrectResult()
    {
        var localTriggers = GetLocalConfigs();
        var remoteTriggers = GetRemoteConfigs();

        Mock<ITriggersClient> mockTriggersClient = new();
        var handler = new TriggersDeploymentHandler(mockTriggersClient.Object);

        mockTriggersClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteTriggers.ToList());

        var actualRes = await handler.DeployAsync(
            localTriggers
        );

        Assert.Contains(localTriggers.FirstOrDefault(l => l.Id == "id1"), actualRes.Updated);
        Assert.Contains(localTriggers.FirstOrDefault(l => l.Id == "id1"), actualRes.Deployed);
        Assert.Contains(localTriggers.FirstOrDefault(l => l.Id == "id2"), actualRes.Created);
        Assert.Contains(localTriggers.FirstOrDefault(l => l.Id == "id2"), actualRes.Deployed);
        Assert.Contains(localTriggers.FirstOrDefault(l => l.Id == "id3"), actualRes.Created);
        Assert.Contains(localTriggers.FirstOrDefault(l => l.Id == "id3"), actualRes.Deployed);
    }

    [Test]
    public async Task DeployAsync_CreateCallsMade()
    {
        var localTriggers = GetLocalConfigs();
        var remoteTriggers = GetRemoteConfigs();

        Mock<ITriggersClient> mockTriggersClient = new();
        var handler = new TriggersDeploymentHandler(mockTriggersClient.Object);

        mockTriggersClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteTriggers.ToList());

        var actualRes = await handler.DeployAsync(
            localTriggers
        );

        mockTriggersClient
            .Verify(
                c => c.Create(
                    It.Is<ITriggerConfig>(l => l.Id == "id2")),
                Times.Once);
        mockTriggersClient
            .Verify(
                c => c.Create(
                    It.Is<ITriggerConfig>(l => l.Id == "id3")),
                Times.Once);
    }

    [Test]
    public async Task DeployAsync_UpdateCallsMade()
    {
        var localTriggers = GetLocalConfigs();
        var remoteTriggers = GetRemoteConfigs();

        Mock<ITriggersClient> mockTriggersClient = new();
        var handler = new TriggersDeploymentHandler(mockTriggersClient.Object);

        mockTriggersClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteTriggers.ToList());

        var actualRes = await handler.DeployAsync(
            localTriggers
        );

        mockTriggersClient
            .Verify(
                c => c.Update(
                    It.Is<ITriggerConfig>(l => l.Id == "id1")),
                Times.Once);
    }

    [Test]
    public async Task DeployAsync_NoReconcileNoDeleteCalls()
    {
        var localTriggers = GetLocalConfigs();
        var remoteTriggers = GetRemoteConfigs();

        Mock<ITriggersClient> mockTriggersClient = new();
        var handler = new TriggersDeploymentHandler(mockTriggersClient.Object);

        mockTriggersClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteTriggers.ToList());

        var actualRes = await handler.DeployAsync(
            localTriggers
        );

        mockTriggersClient
            .Verify(
                c => c.Delete(
                    It.Is<ITriggerConfig>(l => l.Id == "echo")),
                Times.Never);
    }

    [Test]
    public async Task DeployAsync_ReconcileDeleteCalls()
    {
        var localTriggers = GetLocalConfigs();
        var remoteTriggers = GetRemoteConfigs();

        Mock<ITriggersClient> mockTriggersClient = new();
        var handler = new TriggersDeploymentHandler(mockTriggersClient.Object);

        mockTriggersClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteTriggers.ToList());

        var actualRes = await handler.DeployAsync(
            localTriggers,
            reconcile: true
        );

        mockTriggersClient
            .Verify(
                c => c.Delete(
                    It.Is<ITriggerConfig>(l => l.Id == "id4")),
                Times.Once);
    }


    [Test]
    public async Task DeployAsync_DryRunNoCalls()
    {
        var localTriggers = GetLocalConfigs();
        var remoteTriggers = GetRemoteConfigs();

        Mock<ITriggersClient> mockTriggersClient = new();
        var handler = new TriggersDeploymentHandler(mockTriggersClient.Object);

        mockTriggersClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteTriggers.ToList());

        var actualRes = await handler.DeployAsync(
            localTriggers,
            true
        );

        mockTriggersClient
            .Verify(
                c => c.Create(
                    It.IsAny<ITriggerConfig>()),
                Times.Never);

        mockTriggersClient
            .Verify(
                c => c.Update(
                    It.IsAny<ITriggerConfig>()),
                Times.Never);

        mockTriggersClient
            .Verify(
                c => c.Delete(
                    It.IsAny<ITriggerConfig>()),
                Times.Never);
    }

    [Test]
    public async Task DeployAsync_DryRunCorrectResult()
    {
        var localTriggers = GetLocalConfigs();
        var remoteTriggers = GetRemoteConfigs();

        Mock<ITriggersClient> mockTriggersClient = new();
        var handler = new TriggersDeploymentHandler(mockTriggersClient.Object);

        mockTriggersClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteTriggers.ToList());

        var actualRes = await handler.DeployAsync(
            localTriggers,
            dryRun: true
        );

        Assert.Contains(localTriggers.FirstOrDefault(l => l.Id == "id1"), actualRes.Updated);
        Assert.Contains(localTriggers.FirstOrDefault(l => l.Id == "id2"), actualRes.Created);
        Assert.Contains(localTriggers.FirstOrDefault(l => l.Id == "id3"), actualRes.Created);
        Assert.AreEqual(0, actualRes.Deployed.Count);
    }

    [Test]
    public async Task DeployAsync_UpdatePassesActionScopeTypeToClient()
    {
        var localTriggers = new List<ITriggerConfig>
        {
            TriggerConfig.CreateWithId("placeholder", "name1", "eventType", "cloud-code", "actionUrn", "", actionScopeType: "Player", path: "path1"),
            TriggerConfig.CreateWithId("id2", "name2", "eventType", "cloud-code", "actionUrn", "", path: "path2"),
            TriggerConfig.CreateWithId("id3", "name3", "eventType", "cloud-code", "actionUrn", "data['someId'] == 'thisId'", path: "path3"),
        };
        var remoteTriggers = GetRemoteConfigs();

        Mock<ITriggersClient> mockTriggersClient = new();
        var handler = new TriggersDeploymentHandler(mockTriggersClient.Object);

        mockTriggersClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteTriggers.ToList());

        await handler.DeployAsync(localTriggers);

        mockTriggersClient.Verify(
            c => c.Update(It.Is<ITriggerConfig>(t => t.Name == "name1" && t.ActionScopeType == "Player")),
            Times.Once);
    }

    [Test]
    public async Task DeployAsync_CreatePassesActionScopeTypeToClient()
    {
        var localTriggers = new List<ITriggerConfig>
        {
            TriggerConfig.CreateWithId("id1", "name1", "eventType", "cloud-code", "actionUrn", "", path: "path1"),
            TriggerConfig.CreateWithId("id2", "name2", "eventType", "cloud-code", "actionUrn", "", actionScopeType: "MultiplayerSession", path: "path2"),
            TriggerConfig.CreateWithId("id3", "name3", "eventType", "cloud-code", "actionUrn", "data['someId'] == 'thisId'", path: "path3"),
        };
        var remoteTriggers = GetRemoteConfigs();

        Mock<ITriggersClient> mockTriggersClient = new();
        var handler = new TriggersDeploymentHandler(mockTriggersClient.Object);

        mockTriggersClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteTriggers.ToList());

        await handler.DeployAsync(localTriggers);

        mockTriggersClient.Verify(
            c => c.Create(It.Is<ITriggerConfig>(t => t.Name == "name2" && t.ActionScopeType == "MultiplayerSession")),
            Times.Once);
    }

    [Test]
    public async Task DeployAsync_DuplicateIdNotDeleted()
    {
        var localTriggers = GetLocalConfigs();
        localTriggers.Add(
            TriggerConfig.CreateWithId("id3x", "name3", "eventType", "cloud-code", "actionUrn", "", path: "otherpath.tr"));
        var remoteTriggers = GetRemoteConfigs();

        Mock<ITriggersClient> mockTriggersClient = new();
        var handler = new TriggersDeploymentHandler(mockTriggersClient.Object);

        mockTriggersClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteTriggers.ToList());

        var actualRes = await handler.DeployAsync(
            localTriggers,
            dryRun: true
        );

        Assert.Contains(localTriggers.FirstOrDefault(l => l.Id == "id3x"), actualRes.Failed);
        Assert.Contains(localTriggers.FirstOrDefault(l => l.Id == "id3"), actualRes.Failed);
    }

    static List<ITriggerConfig> GetLocalConfigs()
    {
        var triggers = new List<ITriggerConfig>()
        {
            TriggerConfig.CreateWithId("id1", "name1", "eventType", "cloud-code", "actionUrn", "", path: "path1"),
            TriggerConfig.CreateWithId("id2", "name2", "eventType", "cloud-code", "actionUrn", "", path: "path2"),
            TriggerConfig.CreateWithId("id3", "name3", "eventType", "cloud-code", "actionUrn", "data['someId'] == 'thisId'", path: "path3"),
        };
        return triggers;
    }

    static IReadOnlyList<ITriggerConfig> GetRemoteConfigs()
    {

        var triggers = new List<ITriggerConfig>()
        {
            TriggerConfig.CreateWithId("id1", "name1", "eventType", "cloud-code", "actionUrn", "", path: "Remote"),
            TriggerConfig.CreateWithId("id4", "name4", "eventType", "cloud-code", "actionUrn", "data['someId'] == 'thisId'", path: "Remote"),
        };
        return triggers;
    }
}
