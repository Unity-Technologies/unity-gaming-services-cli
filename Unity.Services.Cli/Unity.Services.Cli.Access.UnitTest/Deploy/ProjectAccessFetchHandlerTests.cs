using Moq;
using NUnit.Framework;
using Unity.Services.Access.Authoring.Core.Fetch;
using Unity.Services.Access.Authoring.Core.IO;
using Unity.Services.Access.Authoring.Core.Json;
using Unity.Services.Access.Authoring.Core.Model;
using Unity.Services.Access.Authoring.Core.Service;
using Unity.Services.Access.Authoring.Core.Validations;
using Unity.Services.Cli.Access.UnitTest.Utils;

namespace Unity.Services.Cli.Access.UnitTest.Deploy;

public class ProjectAccessFetchHandlerTests
{
    [Test]
    public async Task FetchAsyncWithReconcile_WithNoExistingContent()
    {
        var defaultFileWithNoContent = new List<IProjectAccessFile>()
        {
            TestMocks.GetProjectAccessFile("./project-statements.ac", new List<AccessControlStatement>())
        };
        var remoteStatements = GetRemoteAuthoringStatements();

        Mock<IProjectAccessClient> mockProjectAccessClient = new();
        IProjectAccessConfigValidator projectConfigValidator = new ProjectAccessConfigValidator();
        Mock<IFileSystem> mockFileSystem = new();
        Mock<IJsonConverter> jsonConverter = new();

        var handler = new ProjectAccessFetchHandler(
            mockProjectAccessClient.Object,
            mockFileSystem.Object,
            jsonConverter.Object,
            projectConfigValidator
        );

        mockProjectAccessClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(remoteStatements.ToList());

        var result = await handler.FetchAsync("./", defaultFileWithNoContent, dryRun: false, reconcile: true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Created, Has.Count.EqualTo(1));
            Assert.That(result.Created[0].Sid, Is.EqualTo("allow-access-to-all-services"));
            Assert.That(result.Deleted, Is.Empty);
            Assert.That(result.Updated, Is.Empty);
            Assert.That(result.Fetched, Has.Count.EqualTo(1));
            Assert.That(result.Failed, Is.Empty);
        });
        mockFileSystem
            .Verify(
                f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None),
                Times.Exactly(2));
    }

    [Test]
    public async Task FetchAsyncWithReconcile_WithOutdatedContent()
    {
        var defaultFileWithOutdatedContent = new List<IProjectAccessFile>()
        {
            TestMocks.GetProjectAccessFile(
                "./test_path",
                new List<AccessControlStatement>() {
                    TestMocks.GetAuthoringStatement("allow-access-to-all-services"),
                    TestMocks.GetAuthoringStatement("allow-access-to-cloud-code")
                })
        };
        var remoteStatements = GetRemoteAuthoringStatements();
        remoteStatements.Add(TestMocks.GetAuthoringStatement("deny-access"));

        Mock<IProjectAccessClient> mockProjectAccessClient = new();
        IProjectAccessConfigValidator projectConfigValidator = new ProjectAccessConfigValidator();
        Mock<IFileSystem> mockFileSystem = new();
        Mock<IJsonConverter> mockJsonConverter = new();

        var handler = new ProjectAccessFetchHandler(
            mockProjectAccessClient.Object,
            mockFileSystem.Object,
            mockJsonConverter.Object,
            projectConfigValidator
        );

        mockProjectAccessClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(remoteStatements.ToList());

        var result = await handler.FetchAsync("./", defaultFileWithOutdatedContent, dryRun: false, reconcile: true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Updated, Has.Count.EqualTo(1), "One updated");
            Assert.That(result.Updated[0].Sid, Is.EqualTo("allow-access-to-all-services"));
            Assert.That(result.Updated[0].Effect, Is.EqualTo("Allow"));
            Assert.That(result.Deleted, Has.Count.EqualTo(1), "One deleted");
            Assert.That(result.Deleted[0].Sid, Is.EqualTo("allow-access-to-cloud-code"));
            Assert.That(result.Created, Has.Count.EqualTo(1), "One created");
            Assert.That(result.Fetched, Has.Count.EqualTo(2), "One fetched");
            Assert.That(result.Failed, Is.Empty);
        });

        mockFileSystem
            .Verify(
                f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None),
                Times.Exactly(2));
    }

    [Test]
    public async Task FetchAsyncWithoutReconcile()
    {
        var defaultFileWithOutdatedContent = new List<IProjectAccessFile>()
        {
            TestMocks.GetProjectAccessFile(
                "./test_path",
                new List<AccessControlStatement>() {
                    TestMocks.GetAuthoringStatement("allow-access-to-all-services"),
                    TestMocks.GetAuthoringStatement("allow-access-to-cloud-code")
                })
        };
        var remoteStatements = GetRemoteAuthoringStatements();

        Mock<IProjectAccessClient> mockProjectAccessClient = new();
        IProjectAccessConfigValidator projectConfigValidator = new ProjectAccessConfigValidator();
        Mock<IFileSystem> mockFileSystem = new();
        Mock<IJsonConverter> jsonConverter = new();

        var handler = new ProjectAccessFetchHandler(
            mockProjectAccessClient.Object,
            mockFileSystem.Object,
            jsonConverter.Object,
            projectConfigValidator
        );

        mockProjectAccessClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(remoteStatements.ToList());

        var result = await handler.FetchAsync("./", defaultFileWithOutdatedContent, dryRun: false, reconcile: false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Updated, Has.Count.EqualTo(1));
            Assert.That(result.Updated[0].Sid, Is.EqualTo("allow-access-to-all-services"));
            Assert.That(result.Updated[0].Effect, Is.EqualTo("Allow"));
            Assert.That(result.Deleted, Has.Count.EqualTo(1));
            Assert.That(result.Deleted[0].Sid, Is.EqualTo("allow-access-to-cloud-code"));
            Assert.That(result.Created, Is.Empty);
            Assert.That(result.Fetched, Has.Count.EqualTo(1));
            Assert.That(result.Failed, Is.Empty);
        });

        mockFileSystem
            .Verify(
                f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None),
                Times.Once);
    }

    [Test]
    public async Task FetchAsyncWithDryRun()
    {
        var defaultFileWithOutdatedContent = new List<IProjectAccessFile>()
        {
            TestMocks.GetProjectAccessFile(
                "./test_path",
                new List<AccessControlStatement>() {
                    TestMocks.GetAuthoringStatement("allow-access-to-all-services"),
                    TestMocks.GetAuthoringStatement("allow-access-to-cloud-code")
                })
        };
        var remoteStatements = GetRemoteAuthoringStatements();

        Mock<IProjectAccessClient> mockProjectAccessClient = new();
        IProjectAccessConfigValidator projectConfigValidator = new ProjectAccessConfigValidator();
        Mock<IFileSystem> mockFileSystem = new();
        Mock<IJsonConverter> jsonConverter = new();

        var handler = new ProjectAccessFetchHandler(
            mockProjectAccessClient.Object,
            mockFileSystem.Object,
            jsonConverter.Object,
            projectConfigValidator
        );

        mockProjectAccessClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(remoteStatements.ToList());

        await handler.FetchAsync("./", defaultFileWithOutdatedContent, dryRun: true, reconcile: false);

        mockFileSystem
            .Verify(
                f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None),
                Times.Never);
    }

    [Test]
    public async Task FetchAsyncWithReconcile_NoDefaultFileExists()
    {
        var emptyFiles = new List<IProjectAccessFile>();
        var remoteStatements = GetRemoteAuthoringStatements();

        Mock<IProjectAccessClient> mockProjectAccessClient = new();
        IProjectAccessConfigValidator projectConfigValidator = new ProjectAccessConfigValidator();
        Mock<IFileSystem> mockFileSystem = new();
        Mock<IJsonConverter> jsonConverter = new();

        var handler = new ProjectAccessFetchHandler(
            mockProjectAccessClient.Object,
            mockFileSystem.Object,
            jsonConverter.Object,
            projectConfigValidator
        );

        mockProjectAccessClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(remoteStatements.ToList());

        var result = await handler.FetchAsync("./", emptyFiles, dryRun: false, reconcile: true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Created, Has.Count.EqualTo(1));
            Assert.That(result.Created[0].Sid, Is.EqualTo("allow-access-to-all-services"));
            Assert.That(result.Deleted, Is.Empty);
            Assert.That(result.Updated, Is.Empty);
            Assert.That(result.Failed, Is.Empty);
        });
        mockFileSystem
            .Verify(
                f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None),
                Times.Once);
    }

    [Test]
    public Task FetchAsync_DuplicateAuthoringStatements()
    {
        var defaultFileWithDuplicatedContent = new List<IProjectAccessFile>()
        {
            TestMocks.GetProjectAccessFile(
                "./test_path",
                new List<AccessControlStatement>() {
                    TestMocks.GetAuthoringStatement("allow-access-to-all-services"),
                    TestMocks.GetAuthoringStatement("allow-access-to-all-services")
                })
        };
        var remoteStatements = GetRemoteAuthoringStatements();

        Mock<IProjectAccessClient> mockProjectAccessClient = new();
        IProjectAccessConfigValidator projectConfigValidator = new ProjectAccessConfigValidator();
        Mock<IFileSystem> mockFileSystem = new();
        Mock<IJsonConverter> jsonConverter = new();

        var handler = new ProjectAccessFetchHandler(
            mockProjectAccessClient.Object,
            mockFileSystem.Object,
            jsonConverter.Object,
            projectConfigValidator
        );

        mockProjectAccessClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(remoteStatements.ToList());

        Assert.ThrowsAsync<AggregateException>(
            () => handler.FetchAsync("./", defaultFileWithDuplicatedContent, dryRun: false, reconcile: true));
        return Task.CompletedTask;
    }

    [Test]
    public async Task FetchAsync_DoesNotCreateFileOnUpdate()
    {
        var files = new List<IProjectAccessFile>()
        {
            TestMocks.GetProjectAccessFile(
                "./test_path.ca",
                new List<AccessControlStatement>() {
                    TestMocks.GetAuthoringStatement("allow-access-to-all-services")
                }),
            TestMocks.GetProjectAccessFile(
                "./test_path_2",
                new List<AccessControlStatement>() {
                    TestMocks.GetAuthoringStatement("other")
                })
        };

        var remoteStatements = new List<AccessControlStatement>
        {
            TestMocks.GetAuthoringStatement(
                "allow-access-to-all-services"),
            TestMocks.GetAuthoringStatement(
                "other")
        };

        Mock<IProjectAccessClient> mockProjectAccessClient = new();
        IProjectAccessConfigValidator projectConfigValidator = new ProjectAccessConfigValidator();
        Mock<IFileSystem> mockFileSystem = new();
        Mock<IJsonConverter> jsonConverter = new();

        var handler = new ProjectAccessFetchHandler(
            mockProjectAccessClient.Object,
            mockFileSystem.Object,
            jsonConverter.Object,
            projectConfigValidator
        );

        mockProjectAccessClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(remoteStatements.ToList());

        var rootDirectory = "./";
        var res = await handler.FetchAsync(rootDirectory, files, dryRun: false, reconcile: true);

        var defaultPathName = Path.GetFullPath(Path.Combine(rootDirectory, ProjectAccessFetchHandler.FetchResultName));

        mockFileSystem.Verify(f => f.WriteAllText(defaultPathName, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Test]
    public async Task FetchAsync_ReportsDefaultFileOnCreate()
    {
        var files = new List<IProjectAccessFile>()
        {
            TestMocks.GetProjectAccessFile(
                "./test_path.ca",
                new List<AccessControlStatement>() {
                    TestMocks.GetAuthoringStatement("allow-access-to-all-services")
                })
        };

        var remoteStatements = new List<AccessControlStatement>
        {
            TestMocks.GetAuthoringStatement(
                "allow-access-to-all-services"),
            TestMocks.GetAuthoringStatement(
                "other")
        };

        Mock<IProjectAccessClient> mockProjectAccessClient = new();
        IProjectAccessConfigValidator projectConfigValidator = new ProjectAccessConfigValidator();
        Mock<IFileSystem> mockFileSystem = new();
        Mock<IJsonConverter> jsonConverter = new();

        var handler = new ProjectAccessFetchHandler(
            mockProjectAccessClient.Object,
            mockFileSystem.Object,
            jsonConverter.Object,
            projectConfigValidator
        );

        mockProjectAccessClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(remoteStatements.ToList());

        var rootDirectory = "./";
        var res = await handler.FetchAsync(rootDirectory, files, dryRun: false, reconcile: true);

        var defaultPathName = Path.GetFullPath(Path.Combine(rootDirectory, ProjectAccessFetchHandler.FetchResultName));

        var defaultFile = res.Fetched.FirstOrDefault(f => Path.GetFullPath(f.Path) == defaultPathName);
        Assert.NotNull(defaultFile);
    }

    static List<AccessControlStatement> GetRemoteAuthoringStatements()
    {
        var remoteStatements = new List<AccessControlStatement>
        {
            TestMocks.GetAuthoringStatement(
                "allow-access-to-all-services",
                null,
                "Allow",
                "Player",
                "urn:ugs:testing:/*")
        };
        return remoteStatements;
    }
}
