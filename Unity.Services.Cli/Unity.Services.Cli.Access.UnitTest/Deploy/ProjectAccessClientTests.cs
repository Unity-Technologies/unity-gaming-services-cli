using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Access.Authoring.Core.Model;
using Unity.Services.Cli.Access.Deploy;
using Unity.Services.Cli.Access.Service;
using Unity.Services.Cli.Access.UnitTest.Utils;
using Unity.Services.Gateway.AccessApiV1.Generated.Model;

namespace Unity.Services.Cli.Access.UnitTest.Deploy;

[TestFixture]
public class ProjectAccessClientTests
{
    const string k_TestProjectId = "a912b1fd-541d-42e1-89f2-85436f27aabd";
    const string k_TestEnvironmentId = "6d06a033-8a15-4919-8e8d-a731e08be87c";

    readonly Mock<IAccessService> m_MockAccessService = new();
    ProjectAccessClient? m_ProjectAccessClient;

    [SetUp]
    public void SetUp()
    {
        m_MockAccessService.Reset();
        m_ProjectAccessClient = new ProjectAccessClient(
            m_MockAccessService.Object,
            k_TestProjectId,
            k_TestEnvironmentId,
            CancellationToken.None);
    }

    [Test]
    public void InitializeChangeProperties()
    {
        m_ProjectAccessClient = new ProjectAccessClient(m_MockAccessService.Object);
        Assert.Multiple(() =>
        {
            Assert.That(m_ProjectAccessClient.ProjectId, Is.EqualTo(string.Empty));
            Assert.That(m_ProjectAccessClient.EnvironmentId, Is.EqualTo(string.Empty));
            Assert.That(m_ProjectAccessClient.CancellationToken, Is.EqualTo(CancellationToken.None));
        });
        CancellationToken cancellationToken = new(true);
        m_ProjectAccessClient!.Initialize( k_TestEnvironmentId, k_TestProjectId, cancellationToken);
        Assert.Multiple(() =>
        {
            Assert.That(m_ProjectAccessClient.ProjectId, Is.SameAs(k_TestProjectId));
            Assert.That(m_ProjectAccessClient.EnvironmentId, Is.SameAs(k_TestEnvironmentId));
            Assert.That(m_ProjectAccessClient.CancellationToken, Is.EqualTo(cancellationToken));
        });
    }

    [Test]
    public async Task GetAsyncForPolicyWithNoStatements()
    {
        var policy = new Policy();
        m_MockAccessService.Setup(r => r.GetPolicyAsync(k_TestProjectId, k_TestEnvironmentId, CancellationToken.None)).ReturnsAsync(policy);

        var authoringStatements = new List<AccessControlStatement>()
        {
            TestMocks.GetAuthoringStatement()
        };
        var result = await m_ProjectAccessClient!.GetAsync();

        m_MockAccessService.Verify(ac => ac.GetPolicyAsync(k_TestProjectId, k_TestEnvironmentId, CancellationToken.None), Times.Once);
        Assert.That(result, Has.Exactly(0).Items);
    }

    [Test]
    public async Task GetAsyncForPolicyWithStatements()
    {
        var policy = TestMocks.GetPolicy(new List<Statement>(){TestMocks.GetStatement()});
        m_MockAccessService.Setup(r => r.GetPolicyAsync(k_TestProjectId, k_TestEnvironmentId, CancellationToken.None)).ReturnsAsync(policy);

        var authoringStatements = new List<AccessControlStatement>()
        {
            TestMocks.GetAuthoringStatement()
        };
        var expectedResult = JsonConvert.SerializeObject(authoringStatements);
        var result = JsonConvert.SerializeObject(await m_ProjectAccessClient!.GetAsync());

        m_MockAccessService.Verify(ac => ac.GetPolicyAsync(k_TestProjectId, k_TestEnvironmentId, CancellationToken.None), Times.Once);
        StringAssert.AreEqualIgnoringCase(expectedResult, result);
    }

    [Test]
    public async Task UpsertAsyncSuccessfully()
    {
        var authoringStatements = new List<AccessControlStatement>(){TestMocks.GetAuthoringStatement("sid-1"), TestMocks.GetAuthoringStatement("sid-2")};
        var policy = TestMocks.GetPolicy(new List<Statement>(){TestMocks.GetStatement("sid-1"), TestMocks.GetStatement("sid-2")});
        await m_ProjectAccessClient!.UpsertAsync(authoringStatements);
        m_MockAccessService.Verify(ac => ac.UpsertProjectAccessCaCAsync(k_TestProjectId, k_TestEnvironmentId, policy, CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task DeleteAsyncSuccessfully()
    {
        var authoringStatements = new List<AccessControlStatement>(){TestMocks.GetAuthoringStatement("sid-1"), TestMocks.GetAuthoringStatement("sid-2")};
        var deleteOptions = TestMocks.GetDeleteOptions(new List<string>(){"sid-1", "sid-2"});
        await m_ProjectAccessClient!.DeleteAsync(authoringStatements);
        m_MockAccessService.Verify(ac => ac.DeleteProjectAccessCaCAsync(k_TestProjectId, k_TestEnvironmentId, deleteOptions, CancellationToken.None), Times.Once);
    }
}
