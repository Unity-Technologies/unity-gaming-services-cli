using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Deployment;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.ErrorHandling;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Fetch;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Results;
using FetchResult = Unity.Services.RemoteConfig.Editor.Authoring.Core.Results.FetchResult;
using RemoteConfigFile = Unity.Services.Cli.RemoteConfig.Deploy.RemoteConfigFile;

namespace Unity.Services.Cli.RemoteConfig.UnitTest.Deploy;

[TestFixture]
public class RemoteConfigFetchServiceTests
{
    RemoteConfigFetchService? m_RemoteConfigFetchService;
    const string k_ValidProjectId = "a912b1fd-541d-42e1-89f2-85436f27aabd";
    const string k_ValidEnvironmentId = "00000000-0000-0000-0000-000000000000";
    const string k_DeployFileExtension = ".rc";

    static readonly List<string> k_ValidFilePaths = new()
    {
        "test_a.rc",
        "test_b.rc"
    };

    List<IRemoteConfigFile> m_RemoteConfigFiles = new();

    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ICliRemoteConfigClient> m_MockCliRemoteConfigClient = new();
    readonly Mock<IDeployFileService> m_MockDeployFileService = new();
    readonly Mock<IRemoteConfigScriptsLoader> m_MockRemoteConfigScriptsLoader = new();
    readonly Mock<IRemoteConfigFetchHandler> m_MockRemoteConfigFetchHandler = new();

    readonly Mock<IRemoteConfigServicesWrapper> m_MockRemoteConfigServicesWrapper = new();
    readonly Mock<ILogger> m_MockLogger = new();

    FetchInput m_DefaultInput = new()
    {
        CloudProjectId = k_ValidProjectId ,
        Reconcile = false
    };

    Result m_Result = null!;

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockCliRemoteConfigClient.Reset();
        m_MockDeployFileService.Reset();
        m_MockRemoteConfigScriptsLoader.Reset();
        m_MockRemoteConfigFetchHandler.Reset();
        m_MockRemoteConfigServicesWrapper.Reset();
        m_MockLogger.Reset();

        m_RemoteConfigFetchService =
            new RemoteConfigFetchService(
                m_MockUnityEnvironment.Object,
                m_MockRemoteConfigFetchHandler.Object,
                m_MockCliRemoteConfigClient.Object,
                m_MockDeployFileService.Object,
                m_MockRemoteConfigScriptsLoader.Object);

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(k_ValidEnvironmentId);
        m_MockDeployFileService.Setup(d => d.ListFilesToDeploy(new [] {m_DefaultInput.Path}, k_DeployFileExtension))
            .Returns(k_ValidFilePaths);

        m_RemoteConfigFiles = new List<IRemoteConfigFile>(k_ValidFilePaths.Count);
        foreach (var filePath in k_ValidFilePaths)
        {
            var rcFile = new RemoteConfigFile(filePath, filePath);
            m_RemoteConfigFiles.Add(rcFile);
        }

        var created = new List<RemoteConfigEntry>()
        {
            new RemoteConfigEntry()
            {
                File = new RemoteConfigFile("created file", "created_file"),
                Key = "created key",
                Value = null
            }
        };
        var updated = new List<RemoteConfigEntry>()
        {
            new RemoteConfigEntry()
            {
                File = new RemoteConfigFile("updated file", "updated_file"),
                Key = "updated key",
                Value = null
            }
        };
        var deleted = new List<RemoteConfigEntry>()
        {
            new RemoteConfigEntry()
            {
                File = new RemoteConfigFile("deleted file", "deleted_file"),
                Key = "deleted key",
                Value = null
            }
        };

        m_Result = new FetchResult(
            created,
            updated,
            deleted,
            m_RemoteConfigFiles,
            Array.Empty<IRemoteConfigFile>()
        );
        m_MockRemoteConfigFetchHandler
            .Setup(ex => ex
                .FetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<IRemoteConfigFile>>(),
                    false,
                    false,
                    CancellationToken.None))
            .Returns(Task.FromResult((FetchResult)m_Result));

        m_MockRemoteConfigScriptsLoader
            .Setup(loader =>
                loader.LoadScriptsAsync(It.IsAny<IReadOnlyList<string>>(), CancellationToken.None))
            .Returns(Task.FromResult(new LoadResult(Array.Empty<RemoteConfigFile>(), Array.Empty<RemoteConfigFile>())));
    }

    [Test]
    public async Task FetchAsync_MapsResultProperly()
    {
        var res = await m_RemoteConfigFetchService!.FetchAsync(
            m_DefaultInput,
            (StatusContext)null!,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(res.Deleted, Has.Count.EqualTo(1));
            Assert.That(res.Updated, Has.Count.EqualTo(1));
            Assert.That(res.Created, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task FetchAsync_FailedToLoadIsForwarded()
    {
        var failedRcFileName = "failed.rc";
        m_MockRemoteConfigScriptsLoader
            .Setup(loader =>
                loader.LoadScriptsAsync(It.IsAny<IReadOnlyList<string>>(), CancellationToken.None))
            .Returns(Task.FromResult(
                new LoadResult(Array.Empty<RemoteConfigFile>(),
                    new []{ new RemoteConfigFile(failedRcFileName, failedRcFileName)
                    {
                        Status = new DeploymentStatus(Statuses.FailedToRead, string.Empty, SeverityLevel.Error)
                    }})));

        var res = await m_RemoteConfigFetchService!.FetchAsync(
            m_DefaultInput,
            (StatusContext)null!,
            CancellationToken.None);

        Assert.That(res.Failed, Has.Count.EqualTo(1));
        Assert.That(res.Failed[0].Name, Is.EqualTo(failedRcFileName));
    }
}
