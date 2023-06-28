using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Import;
using Unity.Services.Cli.Authoring.Import.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Authoring.UnitTest.Import;

public class BaseImportTests
{
    readonly string m_FileName = "test.zip";
    readonly string m_EntryName = "test";

    class TestBaseImporter : BaseImporter<int>
    {
        readonly Mock<IZipArchiver>? m_MockZipArchiver;
        readonly Mock<IUnityEnvironment>? m_MockUnityEnvironment;
        public readonly Mock<ILogger>? MockLogger;
        protected override string FileName { get; }
        protected override string EntryName { get; }
        public readonly List<int> RemoteConfigs;
        public readonly List<int> LocalConfigs;
        readonly ConcurrentBag<int> m_DeletedConfigs = new();
        readonly ConcurrentBag<int> m_CreatedConfigs = new();
        readonly ConcurrentBag<int> m_UpdatedConfigs = new();

        public TestBaseImporter(
            Mock<IZipArchiver> mockZipArchiver,
            Mock<IUnityEnvironment> mockUnityEnvironment,
            Mock<ILogger> mockLogger,
            string fileName,
            string entryName)
            : base(mockZipArchiver.Object, mockUnityEnvironment.Object, mockLogger.Object)
        {
            FileName = fileName;
            EntryName = entryName;

            LocalConfigs = Enumerable.Range(1, 5).ToList();
            RemoteConfigs = Enumerable.Range(10, 6).ToList();

            m_MockZipArchiver = mockZipArchiver;
            m_MockUnityEnvironment = mockUnityEnvironment;
            MockLogger = mockLogger;

            m_MockZipArchiver.Setup(
                    e => e.UnzipAsync<int>(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(LocalConfigs.AsEnumerable()));
        }

        public ConcurrentBag<int> GetDeletedConfigs()
        {
            return m_DeletedConfigs;
        }

        public ConcurrentBag<int> GetCreatedConfigs()
        {
            return m_CreatedConfigs;
        }

        protected override Task DeleteConfigAsync(string projectId, string environmentId, int configToDelete, CancellationToken cancellationToken)
        {
            m_DeletedConfigs.Add(configToDelete);
            return Task.CompletedTask;
        }

        protected override Task<IEnumerable<int>> ListConfigsAsync(string cloudProjectId, string environmentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(RemoteConfigs.AsEnumerable());
        }

        protected override Task CreateConfigAsync(string projectId, string environmentId, int config, CancellationToken cancellationToken)
        {
            m_CreatedConfigs.Add(config);
            return Task.CompletedTask;
        }

        protected override Task UpdateConfigAsync(string projectId, string environmentId, int config, CancellationToken cancellationToken)
        {
            m_UpdatedConfigs.Add(config);
            return Task.CompletedTask;
        }

        protected override ImportExportEntry<int> ToImportExportEntry(int value)
        {
            return new ImportExportEntry<int>(value.GetHashCode(), value.ToString(), value);
        }
    }

    [Test]
    public async Task ImportAsync_DryRunCorrectOutput()
    {
        var testBaseImporter = new TestBaseImporter(
            new(),
           new(),
            new(),
            m_FileName,
            m_EntryName);

        var fileName = "other.zip";

        var importInput = new ImportInput
        {
            FileName = fileName,
            DryRun = true
        };

        var result = testBaseImporter.ImportAsync(importInput, CancellationToken.None);

        await result;

        TestsHelper.VerifyLoggerWasCalled(testBaseImporter.MockLogger!, LogLevel.Critical, expectedTimes: Times.Once);
    }

    [Test]
    public async Task ImportAsync_ReconcileWillDeleteRemoteFiles()
    {
        var testBaseImporter = new TestBaseImporter(
            new(),
            new(),
            new(),
            m_FileName,
            m_EntryName);

        var fileName = "other.zip";

        var importInput = new ImportInput
        {
            FileName = fileName,
            Reconcile = true
        };

        var result = testBaseImporter.ImportAsync(importInput, CancellationToken.None);

        await result;

        var expectedDeletedItems = testBaseImporter.RemoteConfigs;
        var actualDeletedItems = testBaseImporter.GetDeletedConfigs();

        foreach (var deletedItem in expectedDeletedItems)
        {
            Assert.IsTrue(actualDeletedItems.Contains(deletedItem));
        }
    }

    [Test]
    public async Task ImportAsync_WillCreateRemoteFiles()
    {
        var testBaseImporter = new TestBaseImporter(
            new(),
            new(),
            new(),
            m_FileName,
            m_EntryName);

        var fileName = "other.zip";

        var importInput = new ImportInput
        {
            FileName = fileName
        };

        var result = testBaseImporter.ImportAsync(importInput, CancellationToken.None);

        await result;

        var expectedCreatedItems = testBaseImporter.LocalConfigs;
        var actualCreateItems = testBaseImporter.GetCreatedConfigs();

        foreach (var deletedItem in expectedCreatedItems)
        {
            Assert.IsTrue(actualCreateItems.Contains(deletedItem));
        }
    }
}
