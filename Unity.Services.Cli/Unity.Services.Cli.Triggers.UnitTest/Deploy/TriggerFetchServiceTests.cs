using NUnit.Framework;
using Moq;
using Newtonsoft.Json;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Triggers.Deploy;
using Unity.Services.Cli.Triggers.Fetch;
using Unity.Services.Cli.Triggers.IO;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Triggers.Authoring.Core.Fetch;
using Unity.Services.Triggers.Authoring.Core.Model;
using Unity.Services.Triggers.Authoring.Core.Service;
using FetchResult = Unity.Services.Triggers.Authoring.Core.Fetch.FetchResult;

namespace Unity.Services.Cli.Triggers.UnitTest.Deploy;

[TestFixture]
public class TriggerFetchServiceTests
{
    TriggersFetchService? m_FetchService;
    readonly Mock<ITriggersClient> m_MockTriggerClient = new();
    readonly Mock<ITriggersFetchHandler> m_MockTriggerFetchHandler = new();
    readonly Mock<ITriggersResourceLoader> m_MockLoader = new();

    [SetUp]
    public void SetUp()
    {
        m_MockTriggerClient.Reset();
        m_FetchService = new TriggersFetchService(
            m_MockTriggerFetchHandler.Object,
            m_MockTriggerClient.Object,
            m_MockLoader.Object);

        var tr1 = new TriggerConfig("tr1", "Trigger1", "EventType1", "cloud-code", "urn:ugs:cloud-code:MyTestScript");
        var tr2 = new TriggerConfig("tr2", "Trigger2", "EventType2", "cloud-code", "urn:ugs:cloud-code:MyTestScript");

        m_MockLoader
            .Setup(
                m =>
                    m.LoadResource(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new TriggersFileItem(new TriggersConfigFile(new List<TriggerConfig>()
                {
                    new("Trigger1", "EventType1", "cloud-code", "urn:ugs:cloud-code:MyTestScript"),
                    new("Trigger2", "EventType2", "cloud-code", "urn:ugs:cloud-code:MyTestScript")
                }), "samplePath"));

        var deployResult = new FetchResult()
        {
            Created = new List<ITriggerConfig> { tr2 },
            Updated = new List<ITriggerConfig>(),
            Deleted = new List<ITriggerConfig>(),
            Fetched = new List<ITriggerConfig> { tr1, tr2 },
            Failed = new List<ITriggerConfig>()
        };
        var fromResult = Task.FromResult(deployResult);

        m_MockTriggerFetchHandler.Setup(
                d => d.FetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<ITriggerConfig>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                ))
            .Returns(fromResult);
    }

    [Test]
    public async Task FetchAsync_MapsResult()
    {
        var input = new FetchInput()
        {
            Path = "dir",
            CloudProjectId = string.Empty
        };
        var res = await m_FetchService!.FetchAsync(
            input,
            new[] { "dir" },
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);

        Assert.AreEqual(1, res.Created.Count);
        Assert.AreEqual(0, res.Updated.Count);
        Assert.AreEqual(0, res.Deleted.Count);
        Assert.AreEqual(2, res.Fetched.Count);
        Assert.AreEqual(0, res.Failed.Count);
    }

    [Test]
    public async Task FetchAsync_MapsFailed()
    {
        var tr1 = new TriggerConfig("tr1", "tr1", "eventType", "cloud-code", "actionUrn");
        var failedTr = new TriggerConfig("tr2", "tr2", "eventType", "cloud-code", "actionUrn");

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

        var input = new FetchInput()
        {
            Path = "dir",
            CloudProjectId = string.Empty
        };
        var res = await m_FetchService!.FetchAsync(
            input,
            new[] { "dir" },
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);

        Assert.AreEqual(1, res.Created.Count);
        Assert.AreEqual(0, res.Updated.Count);
        Assert.AreEqual(0, res.Deleted.Count);
        Assert.AreEqual(1, res.Fetched.Count);
        Assert.AreEqual(1, res.Failed.Count);
    }
}
