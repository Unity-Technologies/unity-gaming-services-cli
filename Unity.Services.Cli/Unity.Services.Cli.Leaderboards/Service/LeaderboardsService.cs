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

    public async Task<ApiResponse<LeaderboardScoresPage>> GetLeaderboardScoresAsync(
        string projectId, string environmentId, string leaderboardId,
        int? offset, int? limit, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        return await m_LeaderboardsApiAsync.GetLeaderboardScoresWithHttpInfoAsync(
            Guid.Parse(projectId), Guid.Parse(environmentId),
            leaderboardId, offset, limit, cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<LeaderboardEntryWithUpdatedTime>> GetLeaderboardPlayerScoreAsync(
        string projectId, string environmentId, string leaderboardId,
        string playerId, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        return await m_LeaderboardsApiAsync.GetLeaderboardPlayerScoreWithHttpInfoAsync(
            Guid.Parse(projectId), Guid.Parse(environmentId),
            leaderboardId, playerId, cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<LeaderboardScores>> GetLeaderboardScoresPlayerRangeAsync(
        string projectId, string environmentId, string leaderboardId,
        string playerId, int? rangeLimit, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        return await m_LeaderboardsApiAsync.GetLeaderboardScoresPlayerRangeWithHttpInfoAsync(
            Guid.Parse(projectId), Guid.Parse(environmentId),
            leaderboardId, playerId, rangeLimit, cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<LeaderboardTierScoresPage>> GetLeaderboardScoresByTierAsync(
        string projectId, string environmentId, string leaderboardId,
        string tierId, int? offset, int? limit, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        return await m_LeaderboardsApiAsync.GetLeaderboardScoresByTierWithHttpInfoAsync(
            Guid.Parse(projectId), Guid.Parse(environmentId),
            leaderboardId, tierId, offset, limit, cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<LeaderboardScoresWithNotFoundPlayerIds>> GetLeaderboardScoresByPlayerIdsAsync(
        string projectId, string environmentId, string leaderboardId,
        List<string> playerIds, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        return await m_LeaderboardsApiAsync.GetLeaderboardScoresByPlayerIdsWithHttpInfoAsync(
            Guid.Parse(projectId), Guid.Parse(environmentId),
            leaderboardId, new LeaderboardPlayerIds(playerIds), cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<object>> DeleteLeaderboardPlayerScoreAsync(
        string projectId, string environmentId, string leaderboardId,
        string playerId, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        return await m_LeaderboardsApiAsync.DeleteLeaderboardPlayerScoreWithHttpInfoAsync(
            Guid.Parse(projectId), Guid.Parse(environmentId),
            leaderboardId, playerId, cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<object>> PurgeLeaderboardPlayerScoresAsync(
        string projectId, string environmentId, string playerId,
        CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        return await m_LeaderboardsApiAsync.DeleteLeaderboardPlayerScoreAllLeaderboardsWithHttpInfoAsync(
            Guid.Parse(projectId), Guid.Parse(environmentId),
            playerId, cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<BucketsPage>> GetLeaderboardBucketsAsync(
        string projectId, string environmentId, string leaderboardId,
        int? offset, int? limit, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        return await m_LeaderboardsApiAsync.GetLeaderboardBucketsWithHttpInfoAsync(
            Guid.Parse(projectId), Guid.Parse(environmentId),
            leaderboardId, offset, limit, cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<LeaderboardScoresPage>> GetLeaderboardBucketScoresAsync(
        string projectId, string environmentId, string leaderboardId,
        string bucketId, int? offset, int? limit, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        return await m_LeaderboardsApiAsync.GetLeaderboardBucketScoresWithHttpInfoAsync(
            Guid.Parse(projectId), Guid.Parse(environmentId),
            leaderboardId, Guid.Parse(bucketId), offset, limit, cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<LeaderboardTierScoresPage>> GetLeaderboardBucketScoresByTierAsync(
        string projectId, string environmentId, string leaderboardId,
        string bucketId, string tierId, int? offset, int? limit, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        return await m_LeaderboardsApiAsync.GetLeaderboardBucketScoresByTierWithHttpInfoAsync(
            Guid.Parse(projectId), Guid.Parse(environmentId),
            leaderboardId, Guid.Parse(bucketId), tierId, offset, limit, cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<LeaderboardVersionScoresPage>> GetLeaderboardVersionScoresAsync(
        string projectId, string environmentId, string leaderboardId,
        string versionId, int? offset, int? limit, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        return await m_LeaderboardsApiAsync.GetLeaderboardVersionScoresWithHttpInfoAsync(
            Guid.Parse(projectId), Guid.Parse(environmentId),
            leaderboardId, versionId, offset, limit, cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<LeaderboardVersionTierScoresPage>> GetLeaderboardVersionScoresByTierAsync(
        string projectId, string environmentId, string leaderboardId,
        string versionId, string tierId, int? offset, int? limit, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        return await m_LeaderboardsApiAsync.GetLeaderboardVersionScoresByTierWithHttpInfoAsync(
            Guid.Parse(projectId), Guid.Parse(environmentId),
            leaderboardId, versionId, tierId, offset, limit, cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<VersionBucketsPage>> GetLeaderboardVersionBucketsAsync(
        string projectId, string environmentId, string leaderboardId,
        string versionId, int? offset, int? limit, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        return await m_LeaderboardsApiAsync.GetLeaderboardVersionBucketsWithHttpInfoAsync(
            Guid.Parse(projectId), Guid.Parse(environmentId),
            leaderboardId, versionId, offset, limit, cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<LeaderboardVersionScoresPage>> GetLeaderboardVersionBucketScoresAsync(
        string projectId, string environmentId, string leaderboardId,
        string versionId, string bucketId, int? offset, int? limit, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        return await m_LeaderboardsApiAsync.GetLeaderboardVersionBucketScoresWithHttpInfoAsync(
            Guid.Parse(projectId), Guid.Parse(environmentId),
            leaderboardId, versionId, Guid.Parse(bucketId), offset, limit, cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<LeaderboardVersionTierScoresPage>> GetLeaderboardVersionBucketScoresByTierAsync(
        string projectId, string environmentId, string leaderboardId,
        string versionId, string bucketId, string tierId, int? offset, int? limit, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        return await m_LeaderboardsApiAsync.GetLeaderboardVersionBucketScoresByTierWithHttpInfoAsync(
            Guid.Parse(projectId), Guid.Parse(environmentId),
            leaderboardId, versionId, Guid.Parse(bucketId), tierId, offset, limit, cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<LeaderboardVersionEntry>> GetLeaderboardVersionPlayerScoreAsync(
        string projectId, string environmentId, string leaderboardId,
        string versionId, string playerId, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        return await m_LeaderboardsApiAsync.GetLeaderboardVersionPlayerScoreWithHttpInfoAsync(
            Guid.Parse(projectId), Guid.Parse(environmentId),
            leaderboardId, versionId, playerId, cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<LeaderboardVersionRange>> GetLeaderboardVersionScoresPlayerRangeAsync(
        string projectId, string environmentId, string leaderboardId,
        string versionId, string playerId, int? rangeLimit, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        return await m_LeaderboardsApiAsync.GetLeaderboardVersionScoresPlayerRangeWithHttpInfoAsync(
            Guid.Parse(projectId), Guid.Parse(environmentId),
            leaderboardId, versionId, playerId, rangeLimit, cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<LeaderboardVersionScoresByPlayerIds>> GetLeaderboardVersionScoresByPlayerIdsAsync(
        string projectId, string environmentId, string leaderboardId,
        string versionId, List<string> playerIds, CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        return await m_LeaderboardsApiAsync.GetLeaderboardScoresByPlayerIdsArchiveVersionWithHttpInfoAsync(
            Guid.Parse(projectId), Guid.Parse(environmentId),
            leaderboardId, versionId, new LeaderboardPlayerIds(playerIds), cancellationToken: cancellationToken);
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
