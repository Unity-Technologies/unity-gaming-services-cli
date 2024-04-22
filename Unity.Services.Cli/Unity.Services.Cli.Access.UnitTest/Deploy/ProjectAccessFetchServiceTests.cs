using Moq;
using NUnit.Framework;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Fetch;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Model;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Results;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Service;
using Unity.Services.Cli.Access.Deploy;
using Unity.Services.Cli.Access.UnitTest.Utils;
using Unity.Services.Cli.Authoring.Input;

namespace Unity.Services.Cli.Access.UnitTest.Deploy;

[TestFixture]

public class ProjectAccessFetchServiceTests
{
    ProjectAccessFetchService? m_FetchService;
    readonly Mock<IProjectAccessClient> m_ProjectAccessClientMock = new();
    readonly Mock<IProjectAccessFetchHandler> m_ProjectAccessFetchHandlerMock = new();
    readonly Mock<IAccessConfigLoader> m_AccessConfigLoaderMock = new();

    [SetUp]
    public void SetUp()
    {
        m_ProjectAccessClientMock.Reset();
        m_FetchService = new ProjectAccessFetchService(
            m_ProjectAccessClientMock.Object,
            m_ProjectAccessFetchHandlerMock.Object,
            m_AccessConfigLoaderMock.Object);


        var statements = new List<AccessControlStatement>(){TestMocks.GetAuthoringStatement()};
        var projectAccessFileOne = TestMocks.GetProjectAccessFile("path-one",  statements);
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

        var fetchResult = new FetchResult(statements,
            new List<AccessControlStatement>(),
            new List<AccessControlStatement>(),
            new List<IProjectAccessFile>(){projectAccessFileTwo},
            new List<IProjectAccessFile>());

        var fromResult = Task.FromResult(fetchResult);

        m_ProjectAccessFetchHandlerMock.Setup(
                handler => handler.FetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<IProjectAccessFile>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    CancellationToken.None
                ))
            .Returns(fromResult);
    }


    [Test]
    public async Task FetchAsync_MapsResult()
    {
        var input = new FetchInput()
        {
            Path = "",
            CloudProjectId = string.Empty
        };

        var result = await m_FetchService!.FetchAsync(
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
            Assert.That(result.Fetched, Has.Count.EqualTo(1));
            Assert.That(result.Failed, Is.Empty);
        });
    }
}
