using NUnit.Framework;
using Moq;
using Newtonsoft.Json;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Triggers.Deploy;
using Unity.Services.Cli.Triggers.IO;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Triggers.Authoring.Core.Deploy;
using Unity.Services.Triggers.Authoring.Core.IO;
using Unity.Services.Triggers.Authoring.Core.Model;
using Unity.Services.Triggers.Authoring.Core.Service;

namespace Unity.Services.Cli.Triggers.UnitTest.Deploy;

[TestFixture]
public class TriggerDeploymentServiceTests
{
    TriggersDeploymentService? m_DeploymentService;
    readonly Mock<ITriggersClient> m_MockTriggerClient = new();
    readonly Mock<ITriggersDeploymentHandler> m_MockTriggerDeploymentHandler = new();
    readonly Mock<ITriggersResourceLoader> m_MockLoader = new();

    [SetUp]
    public void SetUp()
    {
        m_MockTriggerClient.Reset();
        m_DeploymentService = new TriggersDeploymentService(
            m_MockTriggerDeploymentHandler.Object,
            m_MockTriggerClient.Object,
            m_MockLoader.Object);

        var tr1 = new TriggerConfig("tr1", "Trigger1", "eventType", "cloud-code", "actionUrn");
        var tr2 = new TriggerConfig("tr2", "Trigger2", "eventType", "cloud-code", "actionUrn");

        m_MockLoader
            .Setup(
                m =>
                    m.LoadResource(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                () =>
                {
                    var file = JsonConvert.DeserializeObject<TriggersConfigFile>(
                        "{\"Configs\":[\n    {\"Id\":\"tr1\",\"Name\":\"Trigger1\",\"EventType\":\"EventType1\",\"ActionType\":\"cloud-code\",\"ActionUrn\":\"urn:ugs:cloud-code:MyTestScript\"},\n    {\"Id\":\"tr2\",\"Name\":\"Trigger2\",\"EventType\":\"EventType1\",\"ActionType\":\"cloud-code\",\"ActionUrn\":\"urn:ugs:cloud-code:MyTestScript\"},\n    ]}"
                    );
                    return new TriggersFileItem(file!, "samplePath");
                });
        var deployResult = new DeployResult()
        {
            Created = new List<ITriggerConfig> { tr2 },
            Updated = new List<ITriggerConfig>(),
            Deleted = new List<ITriggerConfig>(),
            Deployed = new List<ITriggerConfig> { tr2 },
            Failed = new List<ITriggerConfig>()
        };
        var fromResult = Task.FromResult(deployResult);

        m_MockTriggerDeploymentHandler.Setup(
                d => d.DeployAsync(
                    It.IsAny<IReadOnlyList<ITriggerConfig>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                ))
            .Returns(fromResult);
    }

    [Test]
    public async Task DeployAsync_MapsResult()
    {
        var input = new DeployInput()
        {
            Paths = Array.Empty<string>(),
            CloudProjectId = string.Empty
        };
        var res = await m_DeploymentService!.Deploy(
            input,
            new[]{"file.tr"},
            String.Empty,
            string.Empty,
            null,
            CancellationToken.None);

        Assert.AreEqual(1, res.Created.Count);
        Assert.AreEqual(0, res.Updated.Count);
        Assert.AreEqual(0, res.Deleted.Count);
        Assert.AreEqual(1, res.Deployed.Count);
        Assert.AreEqual(0, res.Failed.Count);
    }

    [Test]
    public async Task DeployAsync_MapsFailed()
    {
        m_MockLoader
            .Setup(
                m =>
                    m.LoadResource(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(() => new TriggersFileItem(new TriggersConfigFile(new List<TriggerConfig>()), "samplePath")
            {
                Status = new DeploymentStatus("Failed to Read", "...", SeverityLevel.Error)
            });

        var input = new DeployInput()
        {
            CloudProjectId = string.Empty
        };
        var res = await m_DeploymentService!.Deploy(
            input,
            new[] { "file.tr" },
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);

        Assert.AreEqual(1, res.Created.Count);
        Assert.AreEqual(0, res.Updated.Count);
        Assert.AreEqual(0, res.Deleted.Count);
        Assert.AreEqual(0, res.Deployed.Count);
        Assert.AreEqual(1, res.Failed.Count);
    }
}
