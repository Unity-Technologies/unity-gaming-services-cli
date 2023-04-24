using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Player.Service;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.PlayerAdminApiV3.Generated.Api;
using Unity.Services.Gateway.PlayerAuthApiV1.Generated.Api;
using Unity.Services.Gateway.PlayerAuthApiV1.Generated.Model;
using PlayerAdminApiException = Unity.Services.Gateway.PlayerAdminApiV3.Generated.Client.ApiException;

namespace Unity.Services.Cli.Player.UnitTest.Service;

public class PlayerServiceTests
{
    const string k_TestAccessToken = "T3st 4cc3ss t0k3n";
    const string k_ValidProjectId = "812a6bdc-5ef3-46b2-b601-383c4acc65d7";
    const string k_ValidplayerId = "oWT3wwsgsVi1Poxav0wloI9l3fTK";
    const string k_InvalidProjectId = "1nv4lid-pr0j3ct-1d";
    const string k_InvalidPlayerId = "1nv4lid-pl4y3r-1d";


    private readonly Mock<IPlayerAuthenticationAdminApiAsync> m_PlayerAdminApiAsync = new();
    private readonly Mock<IDefaultApiAsync> m_PlayerAuthApiAsync = new();
    readonly Mock<IConfigurationValidator> m_ValidatorObject = new();
    readonly Mock<IServiceAccountAuthenticationService> m_AuthenticationServiceObject = new();

    PlayerService? m_PlayerService;


    [SetUp]
    public void SetUp()
    {
        m_AuthenticationServiceObject.Reset();
        m_ValidatorObject.Reset();
        m_PlayerAdminApiAsync.Reset();
        m_PlayerAuthApiAsync.Reset();

        m_AuthenticationServiceObject.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .Returns(Task.FromResult(k_TestAccessToken));

        m_PlayerAdminApiAsync.Setup(a => a.Configuration)
            .Returns(new Gateway.PlayerAdminApiV3.Generated.Client.Configuration());

        m_PlayerAuthApiAsync.Setup(a => a.Configuration)
            .Returns(new Gateway.PlayerAuthApiV1.Generated.Client.Configuration());

        m_PlayerService = new PlayerService(
            m_PlayerAdminApiAsync.Object,
            m_PlayerAuthApiAsync.Object,
            m_ValidatorObject.Object,
            m_AuthenticationServiceObject.Object);
    }


    [Test]
    public async Task AuthorizePlayerService()
    {
        await m_PlayerService!.AuthorizeAuthAdminServiceAsync(CancellationToken.None);
        m_AuthenticationServiceObject.Verify(a => a.GetAccessTokenAsync(CancellationToken.None));
        Assert.That(
            m_PlayerAdminApiAsync.Object.Configuration.DefaultHeaders[
                AccessTokenHelper.HeaderKey], Is.EqualTo(k_TestAccessToken.ToHeaderValue()));
    }

    [Test]
    public async Task AuthorizePlayerAuthService()
    {
        await m_PlayerService!.AuthorizeAuthServiceAsync(CancellationToken.None);
        m_AuthenticationServiceObject.Verify(a => a.GetAccessTokenAsync(CancellationToken.None));
        Assert.That(
            m_PlayerAuthApiAsync.Object.Configuration.DefaultHeaders[
                AccessTokenHelper.HeaderKey], Is.EqualTo(k_TestAccessToken.ToHeaderValue()));
    }

    [Test]
    public void DeletePlayer_NoProjectId()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_PlayerService!.DeleteAsync(k_InvalidProjectId, k_ValidplayerId , CancellationToken.None));

        m_PlayerAdminApiAsync.Verify(
            a => a.DeletePlayerAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void DeletePlayer_NotFound()
    {
        m_PlayerAdminApiAsync.Setup(a => a.DeletePlayerAsync(k_InvalidPlayerId, k_ValidProjectId, It.IsAny<int>(), CancellationToken.None))
            .Throws(new PlayerAdminApiException());

        Assert.ThrowsAsync<PlayerAdminApiException>(
            () => m_PlayerService!.DeleteAsync(k_ValidProjectId, k_InvalidPlayerId , CancellationToken.None));
    }

    [Test]
    public async Task DeletePlayer_Deleted()
    {
        string error;

        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out error))
            .Returns(true);

        await m_PlayerService!.DeleteAsync(k_ValidProjectId, k_ValidplayerId, CancellationToken.None);

        m_PlayerAdminApiAsync.Verify(
            a => a.DeletePlayerAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void CreatePlayer_NoProjectId()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_PlayerService!.CreateAsync(k_InvalidProjectId , CancellationToken.None));

        m_PlayerAuthApiAsync.Verify(
            a => a.AnonymousSignupAsync(
                It.IsAny<string>(),
                It.IsAny<PlayerAuthAnonymousSignupRequestBody>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task CreatePlayer_Created()
    {
        string error;

        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out error))
            .Returns(true);

        await m_PlayerService!.CreateAsync(k_ValidProjectId, CancellationToken.None);

        m_PlayerAuthApiAsync.Verify(
            a => a.AnonymousSignupAsync(
                It.IsAny<string>(),
                It.IsAny<PlayerAuthAnonymousSignupRequestBody>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void EnablePlayer_NoProjectId()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_PlayerService!.EnableAsync(k_InvalidProjectId, k_ValidplayerId , CancellationToken.None));

        m_PlayerAdminApiAsync.Verify(
            a => a.PlayerEnableAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void EnablePlayer_NotFound()
    {
        m_PlayerAdminApiAsync.Setup(a => a.PlayerEnableAsync(k_InvalidPlayerId, k_ValidProjectId, It.IsAny<int>(), CancellationToken.None))
            .Throws(new PlayerAdminApiException());

        Assert.ThrowsAsync<PlayerAdminApiException>(
            () => m_PlayerService!.EnableAsync(k_ValidProjectId, k_InvalidPlayerId , CancellationToken.None));
    }

    [Test]
    public async Task EnablePlayer_Enabled()
    {
        string error;

        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out error))
            .Returns(true);

        await m_PlayerService!.EnableAsync(k_ValidProjectId, k_ValidplayerId, CancellationToken.None);

        m_PlayerAdminApiAsync.Verify(
            a => a.PlayerEnableAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void DisablePlayer_NoProjectId()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_PlayerService!.DisableAsync(k_InvalidProjectId, k_ValidplayerId , CancellationToken.None));

        m_PlayerAdminApiAsync.Verify(
            a => a.PlayerDisableAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void DisablePlayer_NotFound()
    {
        m_PlayerAdminApiAsync.Setup(a => a.PlayerDisableAsync(k_InvalidPlayerId, k_ValidProjectId, It.IsAny<int>(), CancellationToken.None))
            .Throws(new PlayerAdminApiException());

        Assert.ThrowsAsync<PlayerAdminApiException>(
            () => m_PlayerService!.DisableAsync(k_ValidProjectId, k_InvalidPlayerId , CancellationToken.None));
    }

    [Test]
    public async Task DisablePlayer_Disabled()
    {
        string error;

        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out error))
            .Returns(true);

        await m_PlayerService!.DisableAsync(k_ValidProjectId, k_ValidplayerId, CancellationToken.None);

        m_PlayerAdminApiAsync.Verify(
            a => a.PlayerDisableAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void GetPlayer_NoProjectId()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_PlayerService!.GetAsync(k_InvalidProjectId, k_ValidplayerId , CancellationToken.None));

        m_PlayerAdminApiAsync.Verify(
            a => a.GetPlayerAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void GetPlayer_NotFound()
    {
        m_PlayerAdminApiAsync.Setup(a => a.GetPlayerAsync(k_InvalidPlayerId, k_ValidProjectId, It.IsAny<int>(), CancellationToken.None))
            .ThrowsAsync(new PlayerAdminApiException());

        Assert.ThrowsAsync<PlayerAdminApiException>(
            () => m_PlayerService!.GetAsync(k_ValidProjectId, k_InvalidPlayerId , CancellationToken.None));
    }

    [Test]
    public async Task GetPlayer_Returns()
    {
        string error;

        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out error))
            .Returns(true);

        await m_PlayerService!.GetAsync(k_ValidProjectId, k_ValidplayerId, CancellationToken.None);

        m_PlayerAdminApiAsync.Verify(
            a => a.GetPlayerAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void ListPlayer_NoProjectId()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_PlayerService!.ListAsync(k_InvalidProjectId, cancellationToken: CancellationToken.None));

        m_PlayerAdminApiAsync.Verify(
            a => a.ListPlayersAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ListPlayer_Returns()
    {
        string error;

        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out error))
            .Returns(true);

        await m_PlayerService!.ListAsync(k_ValidProjectId, cancellationToken: CancellationToken.None);

        m_PlayerAdminApiAsync.Verify(
            a => a.ListPlayersAsync(
                It.IsAny<string>(),
                null,
                null,
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
