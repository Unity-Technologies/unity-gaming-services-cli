using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Export;
using Unity.Services.Cli.Authoring.Export.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Authoring.UnitTest.Export;

public class BaseExporterTests
{
    readonly Mock<IZipArchiver> m_ZipArchiverMock = new();
    readonly Mock<IUnityEnvironment> m_UnityEnvironment = new();
    readonly Mock<IFileSystem> m_FileSystemMock = new();
    readonly Mock<ILogger> m_LoggerMock = new();

    readonly string m_FileName = "test.zip";
    readonly string m_EntryName = "test";
    TestBaseExporter? m_TestBaseExporter;

    class TestBaseExporter : BaseExporter<int>
    {
        public TestBaseExporter(
            IZipArchiver zipArchiver,
            IUnityEnvironment unityEnvironment,
            IFileSystem fileSystem,
            ILogger logger,
            string fileName,
            string entryName) :
            base(
                zipArchiver,
                unityEnvironment,
                fileSystem,
                logger)
        {
            FileName = fileName;
            EntryName = entryName;
        }

        protected override string FileName { get; }
        protected override string EntryName { get; }

        protected override Task<IEnumerable<int>> ListConfigsAsync(string projectId, string environmentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Range(10, 6));
        }

        protected override ImportExportEntry<int> ToImportExportEntry(int value)
        {
            return new ImportExportEntry<int>(value.GetHashCode(), value.ToString(), value);
        }
    }

    [SetUp]
    public void SetUp()
    {
        m_ZipArchiverMock.Reset();
        m_UnityEnvironment.Reset();
        m_FileSystemMock.Reset();
        m_LoggerMock.Reset();

        m_TestBaseExporter = new TestBaseExporter(
            m_ZipArchiverMock.Object,
            m_UnityEnvironment.Object,
            m_FileSystemMock.Object,
            m_LoggerMock.Object,
            m_FileName,
            m_EntryName);
    }

    [Test]
    public async Task ExportAsync_WillZipCorrectConfigs()
    {
        var fileName = "other.zip";

        var exportInput = new ExportInput
        {
            OutputDirectory = "mock_output_directory",
            FileName = fileName
        };

        m_FileSystemMock.Setup(
            e => e
                .Directory
                .CreateDirectory(It.IsAny<string>()));

        m_FileSystemMock.Setup(
            e => e
                .Path
                .Join(It.IsAny<string?>(), It.IsAny<string?>()));

        m_FileSystemMock.Setup(
                e => e
                    .File
                    .Exists(It.IsAny<string>()))
            .Returns<string>((path) => false);

        var result = m_TestBaseExporter!.ExportAsync(exportInput, CancellationToken.None);

        await result;

        m_ZipArchiverMock.Verify(
            e =>
                e.ZipAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    Enumerable.Range(10, 6),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ExportAsync_DryRunCorrectOutput()
    {
        var fileName = "other.zip";

        var exportInput = new ExportInput
        {
            OutputDirectory = "mock_output_directory",
            FileName = fileName,
            DryRun = true
        };

        var result = m_TestBaseExporter!.ExportAsync(exportInput, CancellationToken.None);

        await result;

        TestsHelper.VerifyLoggerWasCalled(m_LoggerMock, LogLevel.Critical, expectedTimes: Times.Once);
    }

    [Test]
    public void ExportAsync_FailsWhenFileExist()
    {
        var fileName = "other.zip";

        var exportInput = new ExportInput
        {
            OutputDirectory = "mock_output_directory",
            FileName = fileName
        };

        m_FileSystemMock.Setup(
            e => e
                .Directory
                .CreateDirectory(It.IsAny<string>()));

        m_FileSystemMock.Setup(
            e => e
                .Path
                .Join(It.IsAny<string?>(), It.IsAny<string?>()));

        m_FileSystemMock.Setup(
                e => e
                    .File
                    .Exists(It.IsAny<string>()))
            .Returns<string>((path) => true);

        var exception = Assert.ThrowsAsync<CliException>(
            async () =>
            {
                await m_TestBaseExporter!.ExportAsync(exportInput, CancellationToken.None);
            });

        Assert.That(exception!.Message, Is.EqualTo("The filename to export to already exists. Please create a new file"));
    }
}
