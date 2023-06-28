using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.PlayerAdminApiV3.Generated.Api;
using Unity.Services.Gateway.PlayerAdminApiV3.Generated.Model;
using Unity.Services.Gateway.PlayerAuthApiV1.Generated.Api;
using Unity.Services.Gateway.PlayerAuthApiV1.Generated.Model;

namespace Unity.Services.Cli.Player.Service;

class PlayerService : IPlayerService
{
    readonly IPlayerAuthenticationAdminApiAsync m_PlayerAdminApiAsync;
    readonly IDefaultApiAsync m_PlayerAuthApiAsync;
    readonly IServiceAccountAuthenticationService m_ServiceAccountService;
    readonly IConfigurationValidator m_ConfigValidator;
    public PlayerService(IPlayerAuthenticationAdminApiAsync playerAdminApiAsync, IDefaultApiAsync authApiAsync, IConfigurationValidator validator, IServiceAccountAuthenticationService authenticationService)
    {
        m_ConfigValidator = validator;
        m_ServiceAccountService = authenticationService;
        m_PlayerAdminApiAsync = playerAdminApiAsync;
        m_PlayerAuthApiAsync = authApiAsync;
    }
    public async Task DeleteAsync(string projectId, string playerId, CancellationToken cancellationToken = default)
    {
        await AuthorizeAuthAdminServiceAsync(cancellationToken);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        await m_PlayerAdminApiAsync.DeletePlayerAsync(playerId, projectId, cancellationToken: cancellationToken);
    }

    public async Task EnableAsync(string projectId, string playerId, CancellationToken cancellationToken = default)
    {
        await AuthorizeAuthAdminServiceAsync(cancellationToken);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        await m_PlayerAdminApiAsync.PlayerEnableAsync(playerId, projectId, cancellationToken: cancellationToken);
    }

    public async Task DisableAsync(string projectId, string playerId, CancellationToken cancellationToken = default)
    {
        await AuthorizeAuthAdminServiceAsync(cancellationToken);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        await m_PlayerAdminApiAsync.PlayerDisableAsync(playerId, projectId, cancellationToken: cancellationToken);
    }

    public async Task<PlayerAuthAuthenticationResponse> CreateAsync(string projectId, CancellationToken cancellationToken = default)
    {
        await AuthorizeAuthServiceAsync(cancellationToken);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        var req = new PlayerAuthAnonymousSignupRequestBody();
        return await m_PlayerAuthApiAsync.AnonymousSignupAsync(projectId, req, cancellationToken: cancellationToken);
    }

    public async Task<PlayerAuthPlayerProjectResponse> GetAsync(string projectId, string playerId, CancellationToken cancellationToken = default)
    {
        await AuthorizeAuthAdminServiceAsync(cancellationToken);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        return await m_PlayerAdminApiAsync.GetPlayerAsync(playerId, projectId, cancellationToken: cancellationToken);
    }

    public async Task<PlayerAuthListProjectUserResponse> ListAsync(string projectId, int? limit = null, string? page = null, CancellationToken cancellationToken = default)
    {
        await AuthorizeAuthAdminServiceAsync(cancellationToken);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        return await m_PlayerAdminApiAsync.ListPlayersAsync(projectId, limit, page, cancellationToken: cancellationToken);
    }

    internal async Task AuthorizeAuthServiceAsync(CancellationToken cancellationToken = default)
    {
        var token = await m_ServiceAccountService.GetAccessTokenAsync(cancellationToken);
        m_PlayerAuthApiAsync.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }

    internal async Task AuthorizeAuthAdminServiceAsync(CancellationToken cancellationToken = default)
    {
        var token = await m_ServiceAccountService.GetAccessTokenAsync(cancellationToken);
        m_PlayerAdminApiAsync.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }
}
