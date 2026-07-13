using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.ObservabilityApiV1.Generated.Api;
using Unity.Services.Gateway.ObservabilityApiV1.Generated.Client;
using Unity.Services.Gateway.ObservabilityApiV1.Generated.Model;

namespace Unity.Services.Cli.Observability.Service;

public class ObservabilityService : IObservabilityService
{
    readonly IServiceAccountAuthenticationService m_AuthenticationService;
    readonly ILogsApiAsync m_LogsApiAsync;
    readonly IConfigurationValidator m_ConfigValidator;

    public ObservabilityService(
        ILogsApiAsync logsApiAsync,
        IConfigurationValidator validator,
        IServiceAccountAuthenticationService authenticationService)
    {
        m_LogsApiAsync = logsApiAsync;
        m_ConfigValidator = validator;
        m_AuthenticationService = authenticationService;
    }

    public async Task<ApiResponse<LogsResponse>> GetLogsAsync(
        string projectId,
        string environmentId,
        string? from,
        string? to,
        string? query,
        int? offset,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        return await m_LogsApiAsync.GetLogsWithHttpInfoAsync(
            projectId,
            Guid.Parse(environmentId),
            offset,
            limit,
            from,
            to,
            query,
            cancellationToken: cancellationToken);
    }

    internal async Task AuthorizeServiceAsync(CancellationToken cancellationToken = default)
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        m_LogsApiAsync.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }

    internal void ValidateProjectIdAndEnvironmentId(string projectId, string environmentId)
    {
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);
    }
}
