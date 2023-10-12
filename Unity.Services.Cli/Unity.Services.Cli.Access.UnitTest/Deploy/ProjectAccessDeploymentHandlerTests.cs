using Moq;
using NUnit.Framework;
using Unity.Services.Access.Authoring.Core.Deploy;
using Unity.Services.Access.Authoring.Core.Model;
using Unity.Services.Access.Authoring.Core.Service;
using Unity.Services.Access.Authoring.Core.Validations;
using Unity.Services.Cli.Access.UnitTest.Utils;
using InvalidDataException = Unity.Services.Access.Authoring.Core.ErrorHandling.InvalidDataException;

namespace Unity.Services.Cli.Access.UnitTest.Deploy;

[TestFixture]
class ProjectAccessDeploymentHandlerTests
{
    [Test]
    public async Task DeployAsync_CorrectResult()
    {
        var localProjectAccessFiles = GetLocalProjectAccessFiles();
        var remoteStatements = GetRemoteAuthoringStatements();

        Mock<IProjectAccessClient> mockProjectAccessClient = new();
        IProjectAccessConfigValidator projectConfigValidator = new ProjectAccessConfigValidator();
        IProjectAccessMerger projectAccessMerger = new ProjectAccessMerger();

        var handler = new ProjectAccessDeploymentHandler(
            mockProjectAccessClient.Object,
            projectConfigValidator,
            projectAccessMerger
        );

        mockProjectAccessClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(remoteStatements.ToList());

        mockProjectAccessClient
            .Setup(client => client.UpsertAsync(remoteStatements));

        var result = await handler.DeployAsync(localProjectAccessFiles, dryRun: false, reconcile: false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Created, Has.Count.EqualTo(1));
            Assert.That(result.Created[0].Sid, Is.EqualTo("deny-access-to-lobby"));
            Assert.That(result.Updated, Has.Count.EqualTo(1));
            Assert.That(result.Updated[0].Sid, Is.EqualTo("allow-access-to-cloud-save"));
            Assert.That(result.Deleted, Is.Empty);
            Assert.That(result.Deployed, Has.Count.EqualTo(1));
            Assert.That(result.Failed, Is.Empty);
        });
    }

    [Test]
    public async Task DeployAsync_UpsertCallMade()
    {
        var localProjectAccessFiles = GetLocalProjectAccessFiles();

        Mock<IProjectAccessClient> mockProjectAccessClient = new();
        IProjectAccessConfigValidator projectConfigValidator = new ProjectAccessConfigValidator();
        IProjectAccessMerger projectAccessMerger = new ProjectAccessMerger();

        var handler = new ProjectAccessDeploymentHandler(
            mockProjectAccessClient.Object,
            projectConfigValidator,
            projectAccessMerger
        );

        await handler.DeployAsync(localProjectAccessFiles, dryRun: false, reconcile: false);

        mockProjectAccessClient
            .Verify(
                client => client.UpsertAsync(localProjectAccessFiles[0].Statements),
                Times.Once);
    }

    [Test]
    public async Task DeployAsync_NoReconcileNoDeleteCalls()
    {
        var localEmptyProjectAccessFiles = GetLocalEmptyProjectAccessFiles();
        var remoteStatements = GetRemoteAuthoringStatements();

        Mock<IProjectAccessClient> mockProjectAccessClient = new();
        IProjectAccessConfigValidator projectConfigValidator = new ProjectAccessConfigValidator();
        IProjectAccessMerger projectAccessMerger = new ProjectAccessMerger();

        var handler = new ProjectAccessDeploymentHandler(
            mockProjectAccessClient.Object,
            projectConfigValidator,
            projectAccessMerger
        );

        mockProjectAccessClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(remoteStatements.ToList());

        await handler.DeployAsync(localEmptyProjectAccessFiles, dryRun: false, reconcile: false);

        mockProjectAccessClient
            .Verify(
                client => client.DeleteAsync(localEmptyProjectAccessFiles[0].Statements),
                Times.Never);
    }

    [Test]
    public async Task DeployAsync_ReconcileDeleteCalls()
    {
        var localEmptyProjectAccessFiles = GetLocalEmptyProjectAccessFiles();
        var remoteStatements = GetRemoteAuthoringStatements();

        Mock<IProjectAccessClient> mockProjectAccessClient = new();
        IProjectAccessConfigValidator projectConfigValidator = new ProjectAccessConfigValidator();
        IProjectAccessMerger projectAccessMerger = new ProjectAccessMerger();

        var handler = new ProjectAccessDeploymentHandler(
            mockProjectAccessClient.Object,
            projectConfigValidator,
            projectAccessMerger
        );

        mockProjectAccessClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(remoteStatements.ToList());

        await handler.DeployAsync(localEmptyProjectAccessFiles, dryRun: false, reconcile: true);

        mockProjectAccessClient
            .Verify(
                client => client.DeleteAsync(remoteStatements),
                Times.Once);
    }

    [Test]
    public async Task DeployAsync_DryRunNoCalls()
    {
        var localProjectAccessFiles = GetLocalProjectAccessFiles();
        var remoteStatements = GetRemoteAuthoringStatements();

        Mock<IProjectAccessClient> mockProjectAccessClient = new();
        IProjectAccessConfigValidator projectConfigValidator = new ProjectAccessConfigValidator();
        IProjectAccessMerger projectAccessMerger = new ProjectAccessMerger();

        var handler = new ProjectAccessDeploymentHandler(
            mockProjectAccessClient.Object,
            projectConfigValidator,
            projectAccessMerger
        );

        mockProjectAccessClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(remoteStatements.ToList());

        await handler.DeployAsync(localProjectAccessFiles, dryRun: true, reconcile: false);

        mockProjectAccessClient
            .Verify(
                client => client.UpsertAsync(localProjectAccessFiles[0].Statements),
                Times.Never);

        mockProjectAccessClient
            .Verify(
                client => client.DeleteAsync(remoteStatements),
                Times.Never);
    }

    [Test]
    public async Task DeployAsync_DryRunCorrectResult()
    {
        var localProjectAccessFiles = GetLocalProjectAccessFiles();
        var remoteStatements = GetRemoteAuthoringStatements();


        Mock<IProjectAccessClient> mockProjectAccessClient = new();
        IProjectAccessConfigValidator projectConfigValidator = new ProjectAccessConfigValidator();
        IProjectAccessMerger projectAccessMerger = new ProjectAccessMerger();

        var handler = new ProjectAccessDeploymentHandler(
            mockProjectAccessClient.Object,
            projectConfigValidator,
            projectAccessMerger
        );

        mockProjectAccessClient
            .Setup(client => client.GetAsync())
            .ReturnsAsync(remoteStatements.ToList());

        mockProjectAccessClient
            .Setup(client => client.UpsertAsync(remoteStatements));

        var result = await handler.DeployAsync(localProjectAccessFiles, dryRun: true, reconcile: false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Created, Has.Count.EqualTo(1));
            Assert.That(result.Created[0].Sid, Is.EqualTo("deny-access-to-lobby"));
            Assert.That(result.Updated, Has.Count.EqualTo(1));
            Assert.That(result.Updated[0].Sid, Is.EqualTo("allow-access-to-cloud-save"));
            Assert.That(result.Deleted, Is.Empty);
            Assert.That(result.Deployed, Has.Count.EqualTo(1));
            Assert.That(result.Failed, Is.Empty);
        });
    }

    [Test]
    [TestCase("abc", "Deny", "Player", "urn:ugs:*")]
    [TestCase("statement-1", "InvalidEffect", "Player", "urn:ugs:*")]
    [TestCase("statement-3", "Deny", "InvalidPrincipal", "urn:ugs:*")]
    [TestCase("statement-3", "Deny", "Player", "urn")]
    public async Task DeployAsync_InvalidData(string statement, string effect, string principal, string resource)
    {
        var statements = new List<AccessControlStatement>()
        {
            TestMocks.GetAuthoringStatement(statement, null, effect, principal, resource),
        };
        var projectAccessFile = TestMocks.GetProjectAccessFile("path-one", statements);
        var localProjectAccessFiles = new List<IProjectAccessFile>(){projectAccessFile};

        Mock<IProjectAccessClient> mockProjectAccessClient = new();
        IProjectAccessConfigValidator projectConfigValidator = new ProjectAccessConfigValidator();
        IProjectAccessMerger projectAccessMerger = new ProjectAccessMerger();

        var handler = new ProjectAccessDeploymentHandler(
            mockProjectAccessClient.Object,
            projectConfigValidator,
            projectAccessMerger
        );

        var res = await handler.DeployAsync(localProjectAccessFiles, dryRun: false, reconcile: false);

        Assert.IsNotEmpty(res.Failed);

        mockProjectAccessClient
            .Verify(
                client => client.UpsertAsync(localProjectAccessFiles[0].Statements),
                Times.Never);
    }

    static List<AccessControlStatement> GetRemoteAuthoringStatements()
    {
        var remoteStatements = new List<AccessControlStatement>
        {
            TestMocks.GetAuthoringStatement(
                "allow-access-to-cloud-save",
                new List<string>(){"Read"},
                "Allow",
                "Player",
                "urn:ugs:cloud-save:*")
        };
        return remoteStatements;
    }

    static List<IProjectAccessFile> GetLocalProjectAccessFiles()
    {
        var localProjectAccessFiles = new List<IProjectAccessFile>()
        {
            TestMocks.GetProjectAccessFile(
                "./file1",
                new List<AccessControlStatement>() {
                    TestMocks.GetAuthoringStatement("allow-access-to-cloud-save"),
                    TestMocks.GetAuthoringStatement("deny-access-to-lobby")
                }),
        };

        return localProjectAccessFiles;
    }

    static List<IProjectAccessFile> GetLocalEmptyProjectAccessFiles()
    {
        return new List<IProjectAccessFile>()
        {
            TestMocks.GetProjectAccessFile("path1", new List<AccessControlStatement>())
        };
    }
}
