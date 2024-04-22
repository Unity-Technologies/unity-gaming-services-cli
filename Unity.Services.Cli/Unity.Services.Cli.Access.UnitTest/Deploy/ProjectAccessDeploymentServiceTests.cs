using Moq;
using NUnit.Framework;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Deploy;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Model;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Results;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Service;
using Unity.Services.Cli.Access.Deploy;
using Unity.Services.Cli.Access.UnitTest.Utils;
using Unity.Services.Cli.Authoring.Input;

namespace Unity.Services.Cli.Access.UnitTest.Deploy;

[TestFixture]
public class ProjectAccessDeploymentServiceTests
{
    ProjectAccessDeploymentService? m_DeploymentService;
    readonly Mock<IProjectAccessClient> m_ProjectAccessClientMock = new();
    readonly Mock<IProjectAccessDeploymentHandler> m_ProjectAccessDeploymentHandlerMock = new();
    readonly Mock<IAccessConfigLoader> m_AccessConfigLoaderMock = new();

    [SetUp]
    public void SetUp()
    {
        m_ProjectAccessClientMock.Reset();
        m_DeploymentService = new ProjectAccessDeploymentService(
            m_ProjectAccessClientMock.Object,
            m_ProjectAccessDeploymentHandlerMock.Object,
            m_AccessConfigLoaderMock.Object);


        var statements = new List<AccessControlStatement>(){TestMocks.GetAuthoringStatement()};
        var projectAccessFileOne = TestMocks.GetProjectAccessFile("path-one", statements);
        var projectAccessFileTwo = TestMocks.GetProjectAccessFile("path-two", new List<AccessControlStatement>());

        var mockLoad = Task.FromResult(
            new LoadResult(
                loaded: new[]
                {
                    projectAccessFileOne
                },
                failed: Array.Empty<IProjectAccessFile>() ));

        m_AccessConfigLoaderMock
            .Setup(
                loader => loader.LoadFilesAsync(
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()
                ))
            .Returns(mockLoad);

        var deployResult = new DeployResult()
        {
            Created = statements,
            Updated = new List<AccessControlStatement>(),
            Deleted = new List<AccessControlStatement>(),
            Deployed = new List<IProjectAccessFile>
            {
                projectAccessFileTwo
            },
            Failed = new List<IProjectAccessFile>()
        };

        var fromResult = Task.FromResult(deployResult);

        m_ProjectAccessDeploymentHandlerMock.Setup(
                handler => handler.DeployAsync(
                    It.IsAny<IReadOnlyList<IProjectAccessFile>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                ))
            .Returns(fromResult);
    }

    [Test]
    public async Task DeployAsync_MapsResult()
    {
        var input = new DeployInput()
        {
            Paths = Array.Empty<string>(),
            CloudProjectId = string.Empty
        };
        var result = await m_DeploymentService!.Deploy(
            input,
            Array.Empty<string>(),
            String.Empty,
            string.Empty,
            null,
            CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.Created, Has.Count.EqualTo(1));
            Assert.That(result.Updated, Is.Empty);
            Assert.That(result.Deleted, Is.Empty);
            Assert.That(result.Deployed, Has.Count.EqualTo(1));
            Assert.That(result.Failed, Is.Empty);
        });
    }
}
