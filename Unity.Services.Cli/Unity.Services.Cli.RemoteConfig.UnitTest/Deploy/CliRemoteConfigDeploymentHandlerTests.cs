using Moq;
using NUnit.Framework;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.ErrorHandling;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Formatting;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Json;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Service;
using IFileSystem = Unity.Services.RemoteConfig.Editor.Authoring.Core.IO.IFileSystem;
using RemoteConfigFile = Unity.Services.Cli.RemoteConfig.Deploy.RemoteConfigFile;

namespace Unity.Services.Cli.RemoteConfig.UnitTest.Deploy;

[TestFixture]
public class CliRemoteConfigDeploymentHandlerTests
{
    readonly Mock<ICliRemoteConfigClient> m_MockCliRemoteConfigClient = new();
    readonly Mock<IRemoteConfigParser> m_MockRemoteConfigParser = new();
    readonly Mock<IRemoteConfigValidator> m_RemoteConfigValidator = new();
    readonly Mock<IFormatValidator> m_FormatValidator = new();
    readonly Mock<IConfigMerger> m_ConfigMerger = new();
    readonly Mock<IJsonConverter> m_JsonConverter = new();
    readonly Mock<IFileSystem> m_FileReader = new();

    class DeploymentHandlerForTest : CliRemoteConfigDeploymentHandler
    {
        public new void UpdateStatus(
            IRemoteConfigFile remoteConfigFile,
            string status,
            string detail,
            SeverityLevel severityLevel)
        {
            base.UpdateStatus(remoteConfigFile, status, detail, severityLevel);
        }

        public new void UpdateProgress(IRemoteConfigFile remoteConfigFile, float progress)
        {
            base.UpdateProgress(remoteConfigFile, progress);
        }

        public DeploymentHandlerForTest(
            IRemoteConfigClient remoteConfigClient,
            IRemoteConfigParser remoteConfigParser,
            IRemoteConfigValidator remoteConfigValidator,
            IFormatValidator formatValidator,
            IConfigMerger configMerger,
            IJsonConverter jsonConverter,
            IFileSystem fileSystem)
            : base(remoteConfigClient,
                remoteConfigParser,
                remoteConfigValidator,
                formatValidator,
                configMerger,
                jsonConverter,
                fileSystem)
        {
        }
    }

    DeploymentHandlerForTest? m_DeploymentHandlerForTest;

    [SetUp]
    public void SetUp()
    {
        m_DeploymentHandlerForTest
            = new DeploymentHandlerForTest(
                m_MockCliRemoteConfigClient.Object,
                m_MockRemoteConfigParser.Object,
                m_RemoteConfigValidator.Object,
                m_FormatValidator.Object,
                m_ConfigMerger.Object,
                m_JsonConverter.Object,
                m_FileReader.Object
            );
    }

    [Test]
    public Task Deploy_DoesNotThrow()
    {
        m_RemoteConfigValidator.Setup(
            validator => validator.FilterValidEntries(
                It.IsAny<IReadOnlyList<IRemoteConfigFile>>(),
                It.IsAny<IReadOnlyList<RemoteConfigEntry>>(),
                It.IsAny<ICollection<RemoteConfigDeploymentException>>()))
            .Returns(Array.Empty<RemoteConfigEntry>());

        m_ConfigMerger.Setup(
                merger => merger.MergeEntriesToDeploy(
                    It.IsAny<IReadOnlyList<RemoteConfigEntry>>(),
                    It.IsAny<IReadOnlyList<RemoteConfigEntry>>(),
                    It.IsAny<IReadOnlyList<RemoteConfigEntry>>(),
                    It.IsAny<IReadOnlyList<RemoteConfigEntry>>()))
            .Returns(Array.Empty<RemoteConfigEntry>());

        Assert.DoesNotThrowAsync(async () =>
        {
            await m_DeploymentHandlerForTest!.DeployAsync(
                Array.Empty<IRemoteConfigFile>());
        });
        return Task.CompletedTask;
    }

    [Test]
    public void Deploy_StatusMappingWorks()
    {
        var handler = m_DeploymentHandlerForTest!;
        var rcFile = new RemoteConfigFile("rc.rc", "rc.rc")
        {
            Entries = new List<RemoteConfigEntry>()
            {
                new (){ Key = "float", Value = 1.0 },
                new() { Key = "string", Value = "string" },
            }
        };

        handler.UpdateStatus(rcFile, "Test", string.Empty, SeverityLevel.Error);
        Assert.That(rcFile.Status.MessageSeverity, Is.EqualTo(SeverityLevel.Error));

        handler.UpdateStatus(rcFile, "Test", string.Empty, SeverityLevel.Info);
        Assert.That(rcFile.Status.MessageSeverity, Is.EqualTo(SeverityLevel.Info));

        handler.UpdateStatus(rcFile, "Test", string.Empty, SeverityLevel.None);
        Assert.That(rcFile.Status.MessageSeverity, Is.EqualTo(SeverityLevel.None));

        handler.UpdateStatus(rcFile, "Test", string.Empty, SeverityLevel.Info);
        Assert.That(rcFile.Status.MessageSeverity, Is.EqualTo(SeverityLevel.Info));

        handler.UpdateStatus(rcFile, "Test", string.Empty, SeverityLevel.Success);
        Assert.That(rcFile.Status.MessageSeverity, Is.EqualTo(SeverityLevel.Success));

        handler.UpdateStatus(rcFile, "Test", string.Empty, SeverityLevel.Warning);
        Assert.That(rcFile.Status.MessageSeverity, Is.EqualTo(SeverityLevel.Warning));
    }

    [Test]
    public void Deploy_ProgressUpdateWorks()
    {
        var handler = m_DeploymentHandlerForTest!;
        var rcFile = new RemoteConfigFile("rc.rc", "rc.rc")
        {
            Entries = new List<RemoteConfigEntry>()
            {
                new (){ Key = "float", Value = 1.0 },
                new() { Key = "string", Value = "string" },
            }
        };

        Assert.That(rcFile.Progress, Is.EqualTo(0));

        var newProgress = 42f;
        handler.UpdateProgress(rcFile, newProgress);

        Assert.That(rcFile.Progress, Is.EqualTo(newProgress));
    }
}
