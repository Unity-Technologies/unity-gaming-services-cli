using Castle.Core.Resource;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Access.Service;
using Unity.Services.Cli.Access.UnitTest.Utils;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.AccessApiV1.Generated.Api;
using Unity.Services.Gateway.AccessApiV1.Generated.Model;

namespace Unity.Services.Cli.Access.UnitTest.Service;

[TestFixture]
public class AccessServiceTests
{
    readonly Mock<IServiceAccountAuthenticationService> m_AuthenticationServiceObject = new();
    readonly Mock<IPlayerPolicyApi> m_PlayerPolicyApi = new();
    readonly Mock<IProjectPolicyApi> m_ProjectPolicyApi = new();

    AccessService? m_AccessService;
    FileInfo? m_PolicyFile;
    FileInfo? m_DeleteOptionsFile;
    FileInfo? m_WrongFormattedFile;

    static async Task<FileInfo> GetFileInfoObjectAsync(string fileName, string jsonString)
    {
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        await File.WriteAllTextAsync(filePath, jsonString);
        FileInfo file = new FileInfo(filePath);

        return file;
    }

    [SetUp]
    public async Task SetUp()
    {
        m_AuthenticationServiceObject.Reset();
        m_ProjectPolicyApi.Reset();
        m_PlayerPolicyApi.Reset();

        m_DeleteOptionsFile = await GetFileInfoObjectAsync("tmp-delete-options.json", TestValues.DeleteOptionsJson);
        m_PolicyFile = await GetFileInfoObjectAsync("tmp-policy.json", TestValues.PolicyJson);
        m_WrongFormattedFile =
            await GetFileInfoObjectAsync("tmp-wrong-formatted.json", "{\"invalidProperty\":[]}");

        m_AuthenticationServiceObject.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .Returns(Task.FromResult(TestValues.TestAccessToken));

        m_ProjectPolicyApi.Setup(x => x.Configuration).Returns(new Gateway.AccessApiV1.Generated.Client.Configuration());
        m_PlayerPolicyApi.Setup(x => x.Configuration).Returns(new Gateway.AccessApiV1.Generated.Client.Configuration());

        m_AccessService = new AccessService(m_ProjectPolicyApi.Object, m_PlayerPolicyApi.Object, m_AuthenticationServiceObject.Object);

    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        m_PolicyFile?.Delete();
        m_DeleteOptionsFile?.Delete();
        m_WrongFormattedFile?.Delete();
    }

    [Test]
    public async Task AuthorizeServiceAccount()
    {
        await m_AccessService!.AuthorizeServiceAsync(CancellationToken.None);
        m_AuthenticationServiceObject.Verify(a => a.GetAccessTokenAsync(CancellationToken.None));
        Assert.That(
            m_PlayerPolicyApi.Object.Configuration.DefaultHeaders[
                AccessTokenHelper.HeaderKey], Is.EqualTo(TestValues.TestAccessToken.ToHeaderValue()));
    }

    [Test]
    public async Task GetPolicy_Valid()
    {
        await m_AccessService!.GetPolicyAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
            CancellationToken.None);

        m_ProjectPolicyApi.Verify(
            a => a.GetPolicyAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetPlayerPolicy_Valid()
    {
        await m_AccessService!.GetPlayerPolicyAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, TestValues.ValidPlayerId,
            CancellationToken.None);

        m_PlayerPolicyApi.Verify(
            a => a.GetPlayerPolicyAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetAllPlayerPolicies_Valid()
    {
        m_PlayerPolicyApi.Setup(a => a.GetAllPlayerPoliciesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<int>(), CancellationToken.None)).ReturnsAsync(TestMocks.GetPlayerPolicies());


        await m_AccessService!.GetAllPlayerPoliciesAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
            CancellationToken.None);

        m_PlayerPolicyApi.Verify(
            a => a.GetAllPlayerPoliciesAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task UpsertPolicyAsync_Valid()
    {
        m_ProjectPolicyApi.Setup(a => a.UpsertPolicyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Policy>(),
            It.IsAny<int>(), CancellationToken.None));

        await m_AccessService!.UpsertPolicyAsync(
            TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            m_PolicyFile!,
            CancellationToken.None);

        m_ProjectPolicyApi.Verify(
            a => a.UpsertPolicyAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Policy>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void UpsertPolicyAsync_InvalidInput()
    {
        Assert.ThrowsAsync<CliException>(
            () => m_AccessService!.UpsertPolicyAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                m_WrongFormattedFile!,
                CancellationToken.None));
    }

    [Test]
    public async Task UpsertPlayerPolicyAsync_Valid()
    {
        m_PlayerPolicyApi.Setup(a => a.UpsertPlayerPolicyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Policy>(),
            It.IsAny<int>(), CancellationToken.None));

        await m_AccessService!.UpsertPlayerPolicyAsync(
            TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            TestValues.ValidPlayerId,
            m_PolicyFile!,
            CancellationToken.None);

        m_PlayerPolicyApi.Verify(
            a => a.UpsertPlayerPolicyAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Policy>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void UpsertPlayerPolicyAsync_InvalidInput()
    {
        Assert.ThrowsAsync<CliException>(
            () => m_AccessService!.UpsertPlayerPolicyAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                TestValues.ValidPlayerId,
                m_WrongFormattedFile!,
                CancellationToken.None));
    }

    [Test]
    public async Task DeletePolicyStatementsAsync_Valid()
    {
        m_ProjectPolicyApi.Setup(a => a.DeletePolicyStatementsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DeleteOptions>(), It.IsAny<int>(), CancellationToken.None));

        await m_AccessService!.DeletePolicyStatementsAsync(
            TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            m_DeleteOptionsFile!,
            CancellationToken.None);

        m_ProjectPolicyApi.Verify(
            a => a.DeletePolicyStatementsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DeleteOptions>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void DeletePolicyStatementsAsync_InvalidInput()
    {
        Assert.ThrowsAsync<CliException>(
            () => m_AccessService!.DeletePolicyStatementsAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                m_WrongFormattedFile!,
                CancellationToken.None));
    }

    [Test]
    public async Task DeletePlayerPolicyStatementsAsync_Valid()
    {
        m_PlayerPolicyApi.Setup(a => a.DeletePlayerPolicyStatementsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DeleteOptions>(), It.IsAny<int>(), CancellationToken.None));

        await m_AccessService!.DeletePlayerPolicyStatementsAsync(
            TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            TestValues.ValidPlayerId,
            m_DeleteOptionsFile!,
            CancellationToken.None);

        m_PlayerPolicyApi.Verify(
            a => a.DeletePlayerPolicyStatementsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DeleteOptions>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void DeletePlayerPolicyStatementsAsync_InvalidInput()
    {
        Assert.ThrowsAsync<CliException>(
            () => m_AccessService!.DeletePlayerPolicyStatementsAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                TestValues.ValidPlayerId,
                m_WrongFormattedFile!,
                CancellationToken.None));
    }

    [Test]
    public async Task UpsertProjectAccessCaCAsync_Valid()
    {
        var statements = new List<Statement>()
        {
            TestMocks.GetStatement()
        };
        m_ProjectPolicyApi.Setup(a => a.UpsertPolicyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Policy>(),
            It.IsAny<int>(), CancellationToken.None));

        await m_AccessService!.UpsertProjectAccessCaCAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, TestMocks.GetPolicy(statements),
            CancellationToken.None);

        m_ProjectPolicyApi.Verify(
            a => a.UpsertPolicyAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Policy>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DeleteProjectAccessCaCAsync_Valid()
    {
        var statementIDs = new List<string>(){"statement-1"};
        m_ProjectPolicyApi.Setup(a => a.DeletePolicyStatementsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DeleteOptions>(), It.IsAny<int>(), CancellationToken.None));

        await m_AccessService!.DeleteProjectAccessCaCAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, TestMocks.GetDeleteOptions(statementIDs),
            CancellationToken.None);

        m_ProjectPolicyApi.Verify(
            a => a.DeletePolicyStatementsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DeleteOptions>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
