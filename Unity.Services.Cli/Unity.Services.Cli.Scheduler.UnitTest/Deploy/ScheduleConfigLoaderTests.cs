using Moq;
using Unity.Services.Cli.Scheduler.Deploy;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.IO;

namespace Unity.Services.Cli.Scheduler.UnitTest.Deploy;

[TestFixture]
public class ScheduleConfigLoaderTests
{
    SchedulerResourceLoader? m_SchedulesConfigLoader;
    Mock<IFileSystem> m_FileSystem = null!;

    [SetUp]
    public void Setup()
    {
        m_FileSystem = new Mock<IFileSystem>();
        m_SchedulesConfigLoader = new SchedulerResourceLoader(
            m_FileSystem.Object);
    }

    [Test]
    public async Task ConfigLoader_Deserializes()
    {
        var content = @"
        {
          ""Configs"": {
            ""Schedule1"": {
              ""EventName"": ""EventType1"",
              ""Type"": ""recurring"",
              ""Schedule"": ""0 * * * *"",
              ""PayloadVersion"": 1,
              ""Payload"": ""{}"",
              ""StartAt"": ""2026-01-02T03:04:05Z"",
              ""EndAt"": ""2026-02-03T04:05:06Z""
            }
          }
        }";
        m_FileSystem.Setup(f => f.ReadAllText(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        var configs = await m_SchedulesConfigLoader!
            .ReadResource("path", CancellationToken.None);

        var config = configs.Single().entry;

        Assert.That(config.Name, Is.EqualTo("Schedule1"));
        Assert.That(config.EventName, Is.EqualTo("EventType1"));
        Assert.That(config.StartAt, Is.EqualTo(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)));
        Assert.That(config.EndAt, Is.EqualTo(new DateTime(2026, 2, 3, 4, 5, 6, DateTimeKind.Utc)));
        Assert.That(configs.Single().Status.MessageSeverity, Is.EqualTo(SeverityLevel.None));
    }

    [Test]
    public async Task ConfigLoader_ReturnsDuplicateConfigNamesForCoreValidation()
    {
        const string content = """
            {
              "Configs": {
                "Schedule1": {
                  "EventName": "first.event"
                },
                "Schedule1": {
                  "EventName": "second.event"
                }
              }
            }
            """;
        m_FileSystem.Setup(f => f.ReadAllText(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        var configs = await m_SchedulesConfigLoader!
            .ReadResource("path", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(configs.Select(config => config.Name),
                Is.EqualTo(new[] { "Schedule1", "Schedule1" }));
            Assert.That(configs.Select(config => config.entry.EventName),
                Is.EqualTo(new[] { "first.event", "second.event" }));
            Assert.That(configs.Select(config => config.Status.MessageSeverity),
                Is.All.EqualTo(SeverityLevel.None));
        });
    }

    [Test]
    public async Task ConfigLoader_ReportsFailures()
    {
        var content = @"{'";
        m_FileSystem.Setup(f => f.ReadAllText(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        var configs = await m_SchedulesConfigLoader!
            .ReadResource("path", CancellationToken.None);

        Assert.That(configs.Single().Status.MessageSeverity, Is.EqualTo(SeverityLevel.Error));
    }
}
