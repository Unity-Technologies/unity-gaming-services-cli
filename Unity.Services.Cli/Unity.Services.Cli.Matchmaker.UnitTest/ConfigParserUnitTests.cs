using KellermanSoftware.CompareNetObjects;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Matchmaker.Parser;
using Unity.Services.Cli.Matchmaker.Service;
using Unity.Services.Cli.Matchmaker.UnitTest.SampleConfigs;
using Unity.Services.Matchmaker.Authoring.Core.IO;
using Unity.Services.Matchmaker.Authoring.Core.Model;
using Unity.Services.Matchmaker.Authoring.Core.Parser;

namespace Unity.Services.Cli.Matchmaker.UnitTest;

[TestFixture]
class ConfigParserUnitTests
{
    CompareLogic m_CompareLogic = new CompareLogic();

    [SetUp]
    public void Setup()
    {
        m_CompareLogic.Config.ComparePrivateProperties = true;
        m_CompareLogic.Config.ComparePrivateFields = true;
    }

    [Test]
    public async Task EnvironmentConfigParse()
    {
        // Setup
        var coreSampleConfig = new CoreSampleConfig();
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.GetFileName("path/to_file.mme")).Returns("to_file");
        mockFileSystem.Setup(x => x.ReadAllText("path/to_file.mme", default)).ReturnsAsync(JsonSampleConfigLoader.EnvironmentConfig);
        var configParser = new MatchmakerConfigParser(mockFileSystem.Object);

        // Test
        var result = await configParser.Parse(new[] { "path/to_file.mme" }, default);

        // Assert
        Assert.That(result.failed.Count, Is.EqualTo(0));
        Assert.That(result.parsed.Count, Is.EqualTo(1));
        var actual = (EnvironmentConfig)result.parsed[0].Content;
        var compResult = m_CompareLogic.Compare(coreSampleConfig.EnvironmentConfig, actual);
        Assert.IsTrue(compResult.AreEqual, compResult.DifferencesString);
    }

    [Test]
    public async Task EnvironmentConfigSerialize()
    {
        // Setup
        var coreSampleConfig = new CoreSampleConfig();
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.WriteAllText("path/to_file.mme", It.IsAny<string>(), default));
        var configParser = new MatchmakerConfigParser(mockFileSystem.Object);

        // Test
        var result = await configParser.SerializeToFile(coreSampleConfig.EnvironmentConfig, "path/to_file.mme", default);

        // Assert
        Assert.IsTrue(result.Item1);
        Assert.IsEmpty(result.Item2);
        Assert.That(mockFileSystem.Invocations.Count, Is.EqualTo(2));
        Assert.That(mockFileSystem.Invocations[1].Arguments.Count, Is.EqualTo(3));
        var actualJson = (string)mockFileSystem.Invocations[1].Arguments[1];
        Assert.That(actualJson, Is.EqualTo(JsonSampleConfigLoader.EnvironmentConfig));
    }

    [Test]
    public async Task QueueConfigParse()
    {
        // Setup
        var coreSampleConfig = new CoreSampleConfig();
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.GetFileName("path/queue.mmq")).Returns("queue");
        mockFileSystem.Setup(x => x.GetFileName("path/empty_queue.mmq")).Returns("empty_queue");
        mockFileSystem.Setup(x => x.ReadAllText("path/queue.mmq", default)).ReturnsAsync(JsonSampleConfigLoader.QueueConfig);
        mockFileSystem.Setup(x => x.ReadAllText("path/empty_queue.mmq", default)).ReturnsAsync(JsonSampleConfigLoader.EmptyQueueConfig);
        var configParser = new MatchmakerConfigParser(mockFileSystem.Object);

        // Test
        var result = await configParser.Parse(new[] { "path/queue.mmq", "path/empty_queue.mmq" }, default);

        // Assert
        Assert.That(result.failed.Count, Is.EqualTo(0));
        Assert.That(result.parsed.Count, Is.EqualTo(2));
        var actual = (QueueConfig)result.parsed[0].Content;
        var compResult = m_CompareLogic.Compare(coreSampleConfig.QueueConfig, actual);
        Assert.IsTrue(compResult.AreEqual, compResult.DifferencesString);
        actual = (QueueConfig)result.parsed[1].Content;
        compResult = m_CompareLogic.Compare(coreSampleConfig.EmptyQueueConfig, actual);
        Assert.IsTrue(compResult.AreEqual, compResult.DifferencesString);
    }


    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task QueueConfigSerialize(bool emptyConfig)
    {
        // Setup
        var coreSampleConfig = new CoreSampleConfig();
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.WriteAllText("path/to_file.mmq", It.IsAny<string>(), default));
        var configParser = new MatchmakerConfigParser(mockFileSystem.Object);

        // Test
        var result = await configParser.SerializeToFile(
            emptyConfig ? coreSampleConfig.EmptyQueueConfig : coreSampleConfig.QueueConfig
            , "path/to_file.mmq",
            default);

        // Assert
        Assert.IsTrue(result.Item1);
        Assert.IsEmpty(result.Item2);
        Assert.That(mockFileSystem.Invocations.Count, Is.EqualTo(2));
        Assert.That(mockFileSystem.Invocations[1].Arguments.Count, Is.EqualTo(3));
        var actualJson = (string)mockFileSystem.Invocations[1].Arguments[1];
        Assert.That(
            actualJson,
            emptyConfig
                ? Is.EqualTo(JsonSampleConfigLoader.EmptyQueueConfig)
                : Is.EqualTo(JsonSampleConfigLoader.QueueConfig));
    }

    [Test]
    public async Task DeepEqualityCheck()
    {
        // Setup
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.ReadAllText(It.IsAny<string>(), default)).ReturnsAsync((string path, CancellationToken _) =>
        {
            if (path.StartsWith("queue"))
                return JsonSampleConfigLoader.QueueConfig;
            return JsonSampleConfigLoader.EnvironmentConfig;
        }
        );
        var configParser = new MatchmakerConfigParser(mockFileSystem.Object);

        var res = await configParser.Parse(new List<string> { "queue.mmq", "env.mme", }, default);
        var res2 = await configParser.Parse(new List<string> { "queueCopy.mmq", "envCopy.mme" }, default);

        res.failed.AddRange(res2.failed);
        res.parsed.AddRange(res2.parsed);

        // Test
        Assert.That(res.failed.Count, Is.EqualTo(0));
        Assert.That(res.parsed.Count, Is.EqualTo(4));

        var queue = (QueueConfig)res.parsed[0].Content;
        var env = (EnvironmentConfig)res.parsed[1].Content;
        var queueCopy = (QueueConfig)res.parsed[2].Content;
        var envCopy = (EnvironmentConfig)res.parsed[3].Content;

        // Test
        Assert.IsTrue(configParser.IsDeepEqual(queue, queueCopy));
        Assert.IsTrue(configParser.IsDeepEqual(env, envCopy));

        // Modify copies
        queueCopy.MaxPlayersPerTicket = 5;
        envCopy.DefaultQueueName = new QueueName("newQueue");

        // Test
        Assert.IsFalse(configParser.IsDeepEqual(queue, queueCopy));
        Assert.IsFalse(configParser.IsDeepEqual(env, envCopy));
    }

    [Test]
    public async Task ParseDuplicateFails()
    {
        // Setup
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.ReadAllText(It.IsAny<string>(), default)).ReturnsAsync((string path, CancellationToken _) =>
            {
                if (path.StartsWith("queue"))
                    return JsonSampleConfigLoader.QueueConfig;
                return JsonSampleConfigLoader.EnvironmentConfig;
            }
        );
        var configParser = new MatchmakerConfigParser(mockFileSystem.Object);

        var res = await configParser.Parse(new List<string> { "queue.mmq", "env.mme", "queueCopy.mmq", "envCopy.mme" }, default);

        // Test
        Assert.That(res.failed.Count, Is.EqualTo(2));
        Assert.That(res.parsed.Count, Is.EqualTo(2));
        Assert.That(res.failed.Select(f => f.Status.MessageDetail),
        Is.EquivalentTo(new[]
        {
            "Multiple environment config files found in envCopy.mme",
            "Multiple queue config files named DefaultQueueTest found in queueCopy.mmq"
        }));
    }

    [Test]
    public void QueueConfigTemplate()
    {
        // Setup
        var template = new QueueConfigTemplate();

        // Assert
        Assert.AreEqual(JsonSampleConfigLoader.TemplateQueueConfig, template.FileBodyText);
    }

    [Test]
    public async Task ParseInvalidJson()
    {
        // Setup
        var invalidJson = "NotValid";
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.GetFileName("path/to_file.mmq")).Returns("to_file");
        mockFileSystem.Setup(x => x.ReadAllText("path/to_file.mmq", default)).ReturnsAsync(invalidJson);
        var configParser = new MatchmakerConfigParser(mockFileSystem.Object);

        // Test
        var result = await configParser.Parse(new[] { "path/to_file.mmq" }, default);

        // Assert
        Assert.That(result.failed.Count, Is.EqualTo(1));
        Assert.That(result.parsed.Count, Is.EqualTo(0));
        Assert.That(result.failed[0].Status.Message, Is.EqualTo("Invalid json in file path/to_file.mmq"));
    }

    [Test]
    public async Task ParseInvalidPath()
    {
        // Setup
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.GetFileName("path/to_file.mmq")).Returns("to_file");
        mockFileSystem.Setup(x => x.ReadAllText("path/to_file.mmq", default)).Throws(new FileSystemException("path/to_file.mmq", FileSystemException.Action.Read, "File not found"));
        var configParser = new MatchmakerConfigParser(mockFileSystem.Object);

        // Test
        var result = await configParser.Parse(new[] { "path/to_file.mmq" }, default);

        // Assert
        Assert.That(result.failed.Count, Is.EqualTo(1));
        Assert.That(result.parsed.Count, Is.EqualTo(0));
        Assert.That(result.failed[0].Status.MessageDetail, Is.EqualTo("Error trying to read file at path/to_file.mmq : File not found"));
    }

    [Test]
    public async Task ParseEmptyConfig()
    {
        // Setup
        var invalidJson = "";
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.GetFileName("path/to_file.mmq")).Returns("to_file");
        mockFileSystem.Setup(x => x.ReadAllText("path/to_file.mmq", default)).ReturnsAsync(invalidJson);
        var configParser = new MatchmakerConfigParser(mockFileSystem.Object);

        // Test
        var result = await configParser.Parse(new[] { "path/to_file.mmq" }, default);

        // Assert
        Assert.That(result.failed.Count, Is.EqualTo(1));
        Assert.That(result.parsed.Count, Is.EqualTo(0));
        Assert.That(result.failed[0].Status.MessageDetail, Is.EqualTo("Is the file empty ?"));
    }

    [Test]
    public async Task ConfigSerializeException()
    {
        // Setup
        var coreSampleConfig = new CoreSampleConfig();
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.WriteAllText("path/to_file.mme", It.IsAny<string>(), default))
            .Throws(new FileSystemException("path/to_file.mme", FileSystemException.Action.Write, "File not found"));
        var configParser = new MatchmakerConfigParser(mockFileSystem.Object);

        // Test
        var (_, error) = await configParser.SerializeToFile(coreSampleConfig.EnvironmentConfig, "path/to_file.mme", default);

        // Assert
        Assert.That(error, Is.EqualTo(error));
    }
}
