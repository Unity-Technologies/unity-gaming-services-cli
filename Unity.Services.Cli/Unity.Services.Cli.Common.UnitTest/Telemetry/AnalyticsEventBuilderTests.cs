using System.IO.Abstractions;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent.AnalyticEventFactory;

namespace Unity.Services.Cli.Common.UnitTest.Telemetry;

[TestFixture]
class AnalyticsEventBuilderTests
{
    Mock<IAnalyticEventFactory> m_MockFactory = null!;
    Mock<IFileSystem> m_MockFileSystem = null!;
    AnalyticsEventBuilder m_AnalyticsEventBuilder = null!;

    [SetUp]
    public void SetUp()
    {
        m_MockFactory = new Mock<IAnalyticEventFactory>();
        m_MockFileSystem = new Mock<IFileSystem>();
        m_AnalyticsEventBuilder = new AnalyticsEventBuilder(m_MockFactory.Object, m_MockFileSystem.Object);
    }

    [TestCase("a-command")]
    public void SetCommand(string command)
    {
        m_AnalyticsEventBuilder.SetCommandName(command);
        Assert.AreEqual(m_AnalyticsEventBuilder.Command, command);
    }

    [TestCase("option1", "option2", "option3")]
    public void AddOption(params string[] options)
    {
        foreach (var option in options)
        {
            m_AnalyticsEventBuilder.AddCommandOption(option);
        }

        Assert.AreEqual(options.Length, m_AnalyticsEventBuilder.Options.Count);
        for (var i = 0; i < m_AnalyticsEventBuilder.Options.Count; ++i)
        {
            Assert.AreEqual(options[i], m_AnalyticsEventBuilder.Options[i]);
        }
    }

    [TestCase("service1", "service2")]
    public void AddService(params string[] services)
    {
        foreach (var service in services)
        {
            m_AnalyticsEventBuilder.AddAuthoringServiceProcessed(service);
        }

        Assert.AreEqual(services.Length, m_AnalyticsEventBuilder.AuthoringServicesProcessed.Count);
        for (var i = 0; i < m_AnalyticsEventBuilder.AuthoringServicesProcessed.Count; ++i)
        {
            Assert.AreEqual(services[i], m_AnalyticsEventBuilder.AuthoringServicesProcessed[i]);
        }
    }

    [TestCase(new[] { "not-found" }, 0, 0)]
    [TestCase(new[] { "test_output_directory" }, 0, 1)]
    [TestCase(new[] { "test_output_directory\\testfile.txt" }, 1, 0)]
    [TestCase(new[] { "test_output_directory\\testfile.txt", "na", "test_output_directory", "test_output_directory\\testfile2.txt" }, 2, 1)]
    public void SetAuthoringCommandlinePathsInputCount(IReadOnlyList<string> filePaths, int expectedFileCount, int expectedFolderCount)
    {
        m_MockFileSystem.Setup(s => s.Path.GetFullPath("test_output_directory"))
            .Returns("test_output_directory");
        m_MockFileSystem.Setup(s => s.Path.GetFullPath("test_output_directory\\testfile.txt"))
            .Returns("test_output_directory\\testfile.txt");
        m_MockFileSystem.Setup(s => s.Path.GetFullPath("test_output_directory\\testfile2.txt"))
            .Returns("test_output_directory\\testfile2.txt");
        m_MockFileSystem.Setup(s => s.Directory.Exists("test_output_directory")).Returns(true);
        m_MockFileSystem.Setup(s => s.File.Exists("test_output_directory\\testfile.txt")).Returns(true); ;
        m_MockFileSystem.Setup(s => s.File.Exists("test_output_directory\\testfile2.txt")).Returns(true); ;

        m_AnalyticsEventBuilder.SetAuthoringCommandlinePathsInputCount(filePaths);

        Assert.AreEqual(expectedFileCount, m_AnalyticsEventBuilder.FilePathsCommandlineInputCount);
        Assert.AreEqual(expectedFolderCount, m_AnalyticsEventBuilder.FolderPathsCommandlineInputCount);
    }

    [Test]
    public void MetricsAreSent()
    {
        var mockAnalyticEvent = new Mock<IAnalyticEvent>();
        mockAnalyticEvent
            .Setup(e => e.Send())
            .Verifiable();
        m_MockFactory
            .Setup(f => f.CreateMetricEvent())
            .Returns(mockAnalyticEvent.Object)
            .Verifiable();
        m_AnalyticsEventBuilder.SendCommandCompletedEvent();
        mockAnalyticEvent.Verify();
        m_MockFactory.Verify();
    }
}
