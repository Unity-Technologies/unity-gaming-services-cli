using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Export.Input;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.Cli.RemoteConfig.Handlers.ExportImport;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Service;

namespace Unity.Services.Cli.RemoteConfig.UnitTest.ImportExport;


[TestFixture]
class ExportHandlerTests
{
    const string k_TestEnvironment = "00000000-0000-0000-0000-000000000000";
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ICliRemoteConfigClient> m_MockRemoteConfigClient = new();
    readonly Mock<ILogger> m_MockLogger = new();
    readonly Mock<IZipArchiver> m_MockArchiver = new();
    readonly Mock<IFileSystem> m_FileSystemMock = new();
    readonly Mock<ILoadingIndicator> m_MockLoadingIndicator = new();

    RemoteConfigExporter m_RemoteConfigExporter = null!;

    readonly List<RemoteConfigEntryDTO> m_LocalEntries = new()
    {
        new RemoteConfigEntryDTO { key = "local", value = true, type = "bool"}
    };

    ExportInput m_ExportInput = null!;

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockRemoteConfigClient.Reset();
        m_MockLogger.Reset();
        m_FileSystemMock.Reset();

        m_MockUnityEnvironment
            .Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(k_TestEnvironment);

        m_MockArchiver.Setup(
                za => za.UnzipAsync<RemoteConfigEntryDTO>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult((IEnumerable<RemoteConfigEntryDTO>)m_LocalEntries));

        m_RemoteConfigExporter = new RemoteConfigExporter(
            m_MockRemoteConfigClient.Object,
            m_MockArchiver.Object,
            m_MockUnityEnvironment.Object,
            m_FileSystemMock.Object,
            m_MockLogger.Object);

        m_ExportInput = new ExportInput()
        {
            OutputDirectory = "dir",
            FileName = "filename.rczip",
            DryRun = true
        };
    }

    [Test]
    public async Task ExportAsync_CallsLoadingIndicator()
    {
        await ExportHandler.ExportAsync(
            m_ExportInput,
            m_RemoteConfigExporter,
            m_MockLoadingIndicator.Object,
            CancellationToken.None
        );

        m_MockLoadingIndicator
            .Verify(li => li.StartLoadingAsync(
                ExportHandler.LoadingIndicatorMessage,
            It.IsAny<Func<StatusContext?, Task>>()));
    }

    [Test]
    public async Task ExportAsync_Zips()
    {
        m_ExportInput.DryRun = false;

        var remoteEntries = new List<RemoteConfigEntry>()
        {
            new () { Key = "remote", Value = true}
        };

        m_MockRemoteConfigClient
            .Setup(c => c.GetAsync())
            .Returns(Task.FromResult(new GetConfigsResult(true, remoteEntries)));

        var archivePath = Path.Join(m_ExportInput.OutputDirectory, m_ExportInput.FileName);

        m_FileSystemMock.Setup(
            e => e
                .Directory
                .CreateDirectory(It.IsAny<string>()));
        m_FileSystemMock.Setup(
                e => e
                    .Path
                    .Join(It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(archivePath);
        m_FileSystemMock.Setup(
                e => e
                    .File
                    .Exists(It.IsAny<string>()))
            .Returns<string>((path) => false);

        await m_RemoteConfigExporter.ExportAsync(m_ExportInput, CancellationToken.None);

        m_MockArchiver.Verify(za => za.ZipAsync<RemoteConfigEntryDTO>(
            archivePath,
            RemoteConfigConstants.EntryName,
            It.IsAny<IEnumerable<RemoteConfigEntryDTO>>(),
            It.IsAny<CancellationToken>()));
    }

    [Test]
    public void ExportAsync_FailsWithInvalidExtension()
    {
        var fileName = "other.zip";

        m_ExportInput.FileName = fileName;

        var exception = Assert.ThrowsAsync<CliException>(async () =>
        {
            await m_RemoteConfigExporter.ExportAsync(m_ExportInput, CancellationToken.None);
        });
        Assert.That(exception!.Message, Is.EqualTo($"The file-name argument must have the extension '{Path.GetExtension(RemoteConfigConstants.ZipName)}'."));
    }

    [Test]
    [TestCase(true, true, 1, 0, 0, Description = "DryRun still calls Get")]
    [TestCase(false, false, 1, 0, 0, Description = "Does not create if does not exist")]
    [TestCase(false, true, 1, 0, 0, Description = "Does not update if does not exist")]
    public async Task ExportAsync_ApiCalls(bool dryRun, bool exists, int getCalls, int updateCalls, int createCalls)
    {
        m_MockRemoteConfigClient
            .Setup(c => c.GetAsync())
            .Returns(Task.FromResult(new GetConfigsResult(exists, Array.Empty<RemoteConfigEntry>())));

        await m_RemoteConfigExporter.ExportAsync(m_ExportInput, CancellationToken.None);

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
}
