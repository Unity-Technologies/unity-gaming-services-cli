using Newtonsoft.Json;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Api;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Client;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.Service;

public class LeaderboardsService : ILeaderboardsService
{
    readonly IServiceAccountAuthenticationService m_AuthenticationService;
    readonly ILeaderboardsApiAsync m_LeaderboardsApiAsync;
    readonly IConfigurationValidator m_ConfigValidator;

    public LeaderboardsService(ILeaderboardsApiAsync leaderboardsApiAsync, IConfigurationValidator validator,
        IServiceAccountAuthenticationService authenticationService)
    {
        m_LeaderboardsApiAsync = leaderboardsApiAsync;
        m_ConfigValidator = validator;
        m_AuthenticationService = authenticationService;
    }
    public async Task<IEnumerable<UpdatedLeaderboardConfig>> GetLeaderboardsAsync(string projectId, string environmentId, string? cursor, int? limit, CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var response = await m_LeaderboardsApiAsync.GetLeaderboardConfigsAsync(
            Guid.Parse(projectId),
            Guid.Parse(environmentId),
            cursor: cursor,
            limit: limit,
            cancellationToken: cancellationToken);

        return response.Results;
    }

    public async Task<ApiResponse<UpdatedLeaderboardConfig>> GetLeaderboardAsync(string projectId, string environmentId, string leaderboardId, CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var response = await m_LeaderboardsApiAsync.GetLeaderboardConfigWithHttpInfoAsync(
            Guid.Parse(projectId),
            Guid.Parse(environmentId),
            leaderboardId,
            cancellationToken: cancellationToken);

        return response;
    }

    public async Task<ApiResponse<object>> CreateLeaderboardAsync(
        string projectId,
        string environmentId,
        string body,
        CancellationToken cancellationToken)
    {

        var createRequest = DeserializeBody<LeaderboardIdConfig>(body);
        return await CreateLeaderboardAsync(
            projectId,
            environmentId,
            createRequest,
            cancellationToken);
    }

    public async Task<ApiResponse<object>> CreateLeaderboardAsync(
        string projectId,
        string environmentId,
        LeaderboardIdConfig leaderboard,
        CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var response = await m_LeaderboardsApiAsync.CreateLeaderboardWithHttpInfoAsync(
            Guid.Parse(projectId),
            Guid.Parse(environmentId),
            leaderboard,
            cancellationToken: cancellationToken
        );

        return response;
    }

    public async Task<ApiResponse<object>> UpdateLeaderboardAsync(
        string projectId,
        string environmentId,
        string leaderboardId,
        LeaderboardPatchConfig leaderboard,
        CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var response = await m_LeaderboardsApiAsync.UpdateLeaderboardConfigWithHttpInfoAsync(
            Guid.Parse(projectId),
            Guid.Parse(environmentId),
            leaderboardId,
            leaderboard,
            cancellationToken: cancellationToken
        );

        return response;
    }

    public async Task<ApiResponse<object>> UpdateLeaderboardAsync(
        string projectId,
        string environmentId,
        string leaderboardId,
        string body,
        CancellationToken cancellationToken)
    {
        var updateRequest = DeserializeBody<LeaderboardPatchConfig>(body);
        return await UpdateLeaderboardAsync(
            projectId,
            environmentId,
            leaderboardId,
            updateRequest,
            cancellationToken);
    }

    public async Task<ApiResponse<object>> DeleteLeaderboardAsync(string projectId, string environmentId, string leaderboardId, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var response = await m_LeaderboardsApiAsync.DeleteLeaderboardWithHttpInfoAsync(
            Guid.Parse(projectId),
            Guid.Parse(environmentId),
            leaderboardId,
            cancellationToken: cancellationToken
        );

        return response;
    }

    public async Task<ApiResponse<LeaderboardVersionId>> ResetLeaderboardAsync(string projectId, string environmentId, string leaderboardId, bool? archive, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var response = await m_LeaderboardsApiAsync.ResetLeaderboardScoresWithHttpInfoAsync(
            Guid.Parse(projectId),
            Guid.Parse(environmentId),
            leaderboardId,
            archive,
            cancellationToken: cancellationToken
        );

        return response;
    }

    internal async Task AuthorizeServiceAsync(CancellationToken cancellationToken = default)
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        m_LeaderboardsApiAsync.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }

    internal void ValidateProjectIdAndEnvironmentId(string projectId, string environmentId)
    {
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);
    }

    public T DeserializeBody<T>(string value)
    {
        T? result;

        try
        {
            result = JsonConvert.DeserializeObject<T>(value);
        }
        catch (Exception ex)
        {
            throw new CliException(
                "Failed to deserialize object for Leaderboard request: " + ex.Message + " \nContent: " + value, ex,
                ExitCode.HandledError);
        }

        if (result == null)
        {
            throw new CliException("Empty object for Leaderboard request.", ExitCode.HandledError);
        }

        return result;
    }

}
