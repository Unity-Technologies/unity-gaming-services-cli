using System.IO.Abstractions;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Formatting;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Json;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Networking;
using IFileSystem = Unity.Services.RemoteConfig.Editor.Authoring.Core.IO.IFileSystem;

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
        public new  void UpdateStatus(
            IRemoteConfigFile remoteConfigFile,
            string status,
            string detail,
            StatusSeverityLevel severityLevel)
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
    public async Task Deploy_DoesNotThrow()
    {
        Assert.DoesNotThrowAsync(async () =>
        {
            await m_DeploymentHandlerForTest!.DeployAsync(
                Array.Empty<IRemoteConfigFile>(), false);
        });
    }

    [Test]
    public void UpdateStatus_UpdatesContentCorrectly()
    {
        var fileName = "fileName";
        var filePath = "filePath";

        m_DeploymentHandlerForTest!.Contents.Add(new DeployContent(
            fileName,
            "Remote Config",
            filePath,
            0,
            "initialStatus"
            )
        );
        var expectedStatus = "test-status";
        var expectedDetail = "test-detail";
        var expectedProgress = 0.45f;
        var file = new RemoteConfigFile(fileName, filePath, new RemoteConfigFileContent());
        m_DeploymentHandlerForTest.UpdateStatus(
            file,
            expectedStatus,
            expectedDetail,
            StatusSeverityLevel.Info
            );
        m_DeploymentHandlerForTest.UpdateProgress(file, expectedProgress);


        var content = m_DeploymentHandlerForTest.Contents.First();
        Assert.That(content.Status, Is.EqualTo(expectedStatus));
        Assert.That(content.Progress, Is.EqualTo(expectedProgress));
        Assert.That(content.Detail, Is.EqualTo(expectedDetail));
    }
}
