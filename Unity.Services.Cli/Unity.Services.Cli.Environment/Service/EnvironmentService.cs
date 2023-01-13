using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.IdentityApiV1.Generated.Api;
using Unity.Services.Gateway.IdentityApiV1.Generated.Model;

namespace Unity.Services.Cli.Environment;

class EnvironmentService : IEnvironmentService
{
    readonly IEnvironmentApiAsync m_ApiAsync;
    readonly IServiceAccountAuthenticationService m_AuthenticationService;
    readonly IConfigurationValidator m_ConfigValidator;

    public EnvironmentService(IEnvironmentApiAsync defaultApiAsync, IConfigurationValidator validator, IServiceAccountAuthenticationService authenticationService)
    {
        m_ApiAsync = defaultApiAsync;
        m_ConfigValidator = validator;
        m_AuthenticationService = authenticationService;
    }

    /// <inheritdoc cref="IEnvironmentService.ListAsync"/>
    public async Task<IEnumerable<EnvironmentResponse>> ListAsync(string projectId, CancellationToken cancellationToken = default)
    {
        await AuthorizeEnvironmentService(cancellationToken);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);

        var response = await m_ApiAsync.GetEnvironmentsAsync(projectId, cancellationToken: cancellationToken);
        return response?.Results ?? new List<EnvironmentResponse>();
    }

    /// <inheritdoc cref="IEnvironmentService.DeleteAsync"/>
    public async Task DeleteAsync(string projectId, string environmentId, CancellationToken cancellationToken = default)
    {
        await AuthorizeEnvironmentService(cancellationToken);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);
        await m_ApiAsync.DeleteEnvironmentAsync(projectId, environmentId, cancellationToken: cancellationToken);
    }

    /// <inheritdoc cref="IEnvironmentService.AddAsync"/>
    public async Task AddAsync(string environmentName, string projectId, CancellationToken cancellationToken = default)
    {
        await AuthorizeEnvironmentService(cancellationToken);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentName, environmentName);
        await m_ApiAsync.CreateEnvironmentAsync(projectId, new(environmentName), cancellationToken: cancellationToken);
    }

    internal async Task AuthorizeEnvironmentService(CancellationToken cancellationToken = default)
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        m_ApiAsync.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }
}
