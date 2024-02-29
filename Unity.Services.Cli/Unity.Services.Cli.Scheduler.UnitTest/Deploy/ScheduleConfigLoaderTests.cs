using Moq;
using Unity.Services.Cli.Scheduler.Deploy;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Scheduler.Authoring.Core.IO;

namespace Unity.Services.Cli.Scheduler.UnitTest.Deploy;

[TestFixture]
public class ScheduleConfigLoaderTests
{
    ScheduleResourceLoader? m_SchedulesConfigLoader;
    Mock<IFileSystem> m_FileSystem = null!;

    [SetUp]
    public void Setup()
    {
        m_FileSystem = new Mock<IFileSystem>();
        m_SchedulesConfigLoader = new ScheduleResourceLoader(
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
              ""Payload"": ""{}""
            }
          }
        }";
        m_FileSystem.Setup(f => f.ReadAllText(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        var configs = await m_SchedulesConfigLoader!
            .LoadResource("path", CancellationToken.None);

        var config = configs.Content.Configs.First();

        Assert.That(config.Value.Name, Is.EqualTo("Schedule1"));
        Assert.That(config.Value.EventName, Is.EqualTo("EventType1"));
        Assert.That(configs.Status.MessageSeverity, Is.EqualTo(SeverityLevel.None));
    }

    [Test]
    public async Task ConfigLoader_ReportsFailures()
    {
        var content = @"{'";
        m_FileSystem.Setup(f => f.ReadAllText(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        var configs = await m_SchedulesConfigLoader!
            .LoadResource("path", CancellationToken.None);

        Assert.That(configs.Status.MessageSeverity, Is.EqualTo(SeverityLevel.Error));
    }
}
