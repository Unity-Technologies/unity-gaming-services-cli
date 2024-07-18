using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Import.Input;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.Cli.RemoteConfig.Handlers.ExportImport;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Service;

namespace Unity.Services.Cli.RemoteConfig.UnitTest.ImportExport;


[TestFixture]
class ImportHandlerTests
{
    const string k_TestEnvironment = "00000000-0000-0000-0000-000000000000";
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ICliRemoteConfigClient> m_MockRemoteConfigClient = new();
    readonly Mock<ILogger> m_MockLogger = new();
    readonly Mock<IZipArchiver> m_MockArchiver = new();
    readonly Mock<ILoadingIndicator> m_MockLoadingIndicator = new();

    RemoteConfigImporter m_RemoteConfigImporter = null!;

    readonly List<RemoteConfigEntryDTO> m_LocalEntries = new()
    {
        new RemoteConfigEntryDTO { key = "local", value = true, type = "bool"}
    };

    ImportInput m_ImportInput = new()
    {
        InputDirectory = "mock_input_directory"
    };


    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockRemoteConfigClient.Reset();
        m_MockLogger.Reset();

        m_MockUnityEnvironment
            .Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(k_TestEnvironment);

        m_MockArchiver.Setup(
                za => za.UnzipAsync<RemoteConfigEntryDTO>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult((IEnumerable<RemoteConfigEntryDTO>)m_LocalEntries));

        m_RemoteConfigImporter = new RemoteConfigImporter(
            m_MockRemoteConfigClient.Object,
            m_MockArchiver.Object,
            m_MockUnityEnvironment.Object,
            m_MockLogger.Object);

        m_ImportInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory"
        };
    }

    [Test]
    public async Task ImportAsync_CallsLoadingIndicator()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory"
        };

        await ImportHandler.ImportAsync(
            importInput,
            m_RemoteConfigImporter,
            m_MockLoadingIndicator.Object,
            CancellationToken.None
        );

        m_MockLoadingIndicator
            .Verify(li => li.StartLoadingAsync(
                ImportHandler.LoadingIndicatorMessage,
            It.IsAny<Func<StatusContext?, Task>>()));
    }

    [Test]
    public async Task ImportAsync_Unzips()
    {
        m_ImportInput.DryRun = true;

        m_MockRemoteConfigClient
            .Setup(c => c.GetAsync())
            .Returns(Task.FromResult(new GetConfigsResult(true, Array.Empty<RemoteConfigEntry>())));

        await m_RemoteConfigImporter.ImportAsync(m_ImportInput, CancellationToken.None);

        var archivePath = Path.Join(m_ImportInput.InputDirectory, RemoteConfigConstants.ZipName);

        m_MockArchiver.Verify(za => za.UnzipAsync<RemoteConfigEntryDTO>(
            archivePath,
            RemoteConfigConstants.EntryName,
            It.IsAny<CancellationToken>()));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void ImportAsync_FailsWithInvalidExtension(bool dryRun)
    {
        var fileName = "other.zip";

        m_ImportInput.FileName = fileName;
        m_ImportInput.DryRun = dryRun;

        var exception = Assert.ThrowsAsync<CliException>(async () =>
        {
            await m_RemoteConfigImporter.ImportAsync(m_ImportInput, CancellationToken.None);
        });
        Assert.That(exception!.Message, Is.EqualTo($"The file-name argument must have the extension '{Path.GetExtension(RemoteConfigConstants.ZipName)}'."));
    }

    [Test]
    [TestCase(true, true, 1, 0, 0, Description = "DryRun does not create or update")]
    [TestCase(false, false, 1, 0, 1, Description = "Creates if it does not exist")]
    [TestCase(false, true, 1, 1, 0, Description = "Updates if it exists")]
    public async Task ImportAsync_ApiCalls(bool dryRun, bool exists, int getCalls, int updateCalls, int createCalls)
    {
        m_ImportInput.DryRun = dryRun;

        m_MockRemoteConfigClient
            .Setup(c => c.GetAsync())
            .Returns(Task.FromResult(new GetConfigsResult(exists, Array.Empty<RemoteConfigEntry>())));

        await m_RemoteConfigImporter.ImportAsync(m_ImportInput, CancellationToken.None);

        var getCallsTimes = getCalls == 0 ? Times.Never() : Times.Exactly(getCalls);
        var updateCallsTimes = updateCalls == 0 ? Times.Never() : Times.Exactly(updateCalls);
        var createCallsTimes = createCalls == 0 ? Times.Never() : Times.Exactly(createCalls);

        m_MockRemoteConfigClient.Verify(
            c => c.GetAsync(),
            getCallsTimes);
        m_MockRemoteConfigClient.Verify(
            c => c.CreateAsync(It.IsAny<IReadOnlyList<RemoteConfigEntry>>()),
            createCallsTimes);
        m_MockRemoteConfigClient.Verify(
            c => c.UpdateAsync(It.IsAny<IReadOnlyList<RemoteConfigEntry>>()),
            updateCallsTimes);
    }

    [Test]
    public async Task Reconcile_Deletes()
    {
        m_ImportInput.Reconcile = true;

        var remoteEntries = new List<RemoteConfigEntry>()
        {
            new () { Key = "remote", Value = true}
        };

        var sentEntries = new List<RemoteConfigEntry>();

        m_MockRemoteConfigClient
            .Setup(c => c.GetAsync())
            .Returns(Task.FromResult(new GetConfigsResult(true, remoteEntries)));

        m_MockRemoteConfigClient
            .Setup(c => c.UpdateAsync(It.IsAny<IReadOnlyList<RemoteConfigEntry>>()))
            .Callback(
                new Action<IReadOnlyList<RemoteConfigEntry>>(list =>
                {
                    sentEntries.AddRange(list);
                }));

        await m_RemoteConfigImporter.ImportAsync(m_ImportInput, CancellationToken.None);

        m_MockRemoteConfigClient.Verify(
            c => c.UpdateAsync(It.IsAny<IReadOnlyList<RemoteConfigEntry>>()), Times.Once());

        Assert.That(
            sentEntries.SingleOrDefault(e => e.Key == m_LocalEntries[0].key && e.Value == m_LocalEntries[0].value),
            Is.Not.EqualTo(null));
        CollectionAssert.DoesNotContain(sentEntries, remoteEntries[0]);
    }

    [Test]
    public void ImportAsync_SucceedsOnEmptyEnv()
    {
        var importInput = new ImportInput()
        {
            InputDirectory = "mock_input_directory"
        };

        m_MockRemoteConfigClient.Setup(c => c.GetAsync())
            .ReturnsAsync(new GetConfigsResult(false, null));

        Assert.DoesNotThrowAsync(
            async () =>
            {
                await m_RemoteConfigImporter.ImportAsync(
                    importInput,
                    CancellationToken.None
                );
            });
    }
}
