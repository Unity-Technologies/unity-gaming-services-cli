using Unity.Services.Gateway.LeaderboardApiV1.Generated.Client;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.Service;

public interface ILeaderboardsService
{
    public Task<IEnumerable<UpdatedLeaderboardConfig>> GetLeaderboardsAsync(string projectId, string environmentId,
        string? cursor, int? limit, CancellationToken cancellationToken = default);

    Task<ApiResponse<UpdatedLeaderboardConfig>> GetLeaderboardAsync(string projectId, string environmentId, string leaderboardId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> CreateLeaderboardAsync(
        string projectId,
        string environmentId,
        LeaderboardIdConfig leaderboard,
        CancellationToken cancellationToken);

    Task<ApiResponse<object>> CreateLeaderboardAsync(
        string projectId,
        string environmentId,
        string body,
        CancellationToken cancellationToken);

    Task<ApiResponse<object>> UpdateLeaderboardAsync(
        string projectId,
        string environmentId,
        string leaderboardId,
        LeaderboardPatchConfig leaderboard,
        CancellationToken cancellationToken);

    Task<ApiResponse<object>> UpdateLeaderboardAsync(
        string projectId,
        string environmentId,
        string leaderboardId,
        string body,
        CancellationToken cancellationToken);

    Task<ApiResponse<object>> DeleteLeaderboardAsync(string projectId, string environmentId, string leaderboardId,
        CancellationToken cancellationToken);

    Task<ApiResponse<LeaderboardVersionId>> ResetLeaderboardAsync(string projectId, string environmentId,
        string leaderboardId, bool? archive, CancellationToken cancellationToken);

    T DeserializeBody<T>(string value);

    // Scores
    Task<ApiResponse<LeaderboardScoresPage>> GetLeaderboardScoresAsync(string projectId, string environmentId, string leaderboardId, int? offset, int? limit, CancellationToken cancellationToken);
    Task<ApiResponse<LeaderboardEntryWithUpdatedTime>> GetLeaderboardPlayerScoreAsync(string projectId, string environmentId, string leaderboardId, string playerId, CancellationToken cancellationToken);
    Task<ApiResponse<LeaderboardScores>> GetLeaderboardScoresPlayerRangeAsync(string projectId, string environmentId, string leaderboardId, string playerId, int? rangeLimit, CancellationToken cancellationToken);
    Task<ApiResponse<LeaderboardTierScoresPage>> GetLeaderboardScoresByTierAsync(string projectId, string environmentId, string leaderboardId, string tierId, int? offset, int? limit, CancellationToken cancellationToken);
    Task<ApiResponse<LeaderboardScoresWithNotFoundPlayerIds>> GetLeaderboardScoresByPlayerIdsAsync(string projectId, string environmentId, string leaderboardId, List<string> playerIds, CancellationToken cancellationToken);
    Task<ApiResponse<object>> DeleteLeaderboardPlayerScoreAsync(string projectId, string environmentId, string leaderboardId, string playerId, CancellationToken cancellationToken);
    Task<ApiResponse<object>> PurgeLeaderboardPlayerScoresAsync(string projectId, string environmentId, string playerId, CancellationToken cancellationToken);

    // Buckets
    Task<ApiResponse<BucketsPage>> GetLeaderboardBucketsAsync(string projectId, string environmentId, string leaderboardId, int? offset, int? limit, CancellationToken cancellationToken);
    Task<ApiResponse<LeaderboardScoresPage>> GetLeaderboardBucketScoresAsync(string projectId, string environmentId, string leaderboardId, string bucketId, int? offset, int? limit, CancellationToken cancellationToken);
    Task<ApiResponse<LeaderboardTierScoresPage>> GetLeaderboardBucketScoresByTierAsync(string projectId, string environmentId, string leaderboardId, string bucketId, string tierId, int? offset, int? limit, CancellationToken cancellationToken);

    // Versions
    Task<ApiResponse<LeaderboardVersionScoresPage>> GetLeaderboardVersionScoresAsync(string projectId, string environmentId, string leaderboardId, string versionId, int? offset, int? limit, CancellationToken cancellationToken);
    Task<ApiResponse<LeaderboardVersionTierScoresPage>> GetLeaderboardVersionScoresByTierAsync(string projectId, string environmentId, string leaderboardId, string versionId, string tierId, int? offset, int? limit, CancellationToken cancellationToken);
    Task<ApiResponse<VersionBucketsPage>> GetLeaderboardVersionBucketsAsync(string projectId, string environmentId, string leaderboardId, string versionId, int? offset, int? limit, CancellationToken cancellationToken);
    Task<ApiResponse<LeaderboardVersionScoresPage>> GetLeaderboardVersionBucketScoresAsync(string projectId, string environmentId, string leaderboardId, string versionId, string bucketId, int? offset, int? limit, CancellationToken cancellationToken);
    Task<ApiResponse<LeaderboardVersionTierScoresPage>> GetLeaderboardVersionBucketScoresByTierAsync(string projectId, string environmentId, string leaderboardId, string versionId, string bucketId, string tierId, int? offset, int? limit, CancellationToken cancellationToken);
    Task<ApiResponse<LeaderboardVersionEntry>> GetLeaderboardVersionPlayerScoreAsync(string projectId, string environmentId, string leaderboardId, string versionId, string playerId, CancellationToken cancellationToken);
    Task<ApiResponse<LeaderboardVersionRange>> GetLeaderboardVersionScoresPlayerRangeAsync(string projectId, string environmentId, string leaderboardId, string versionId, string playerId, int? rangeLimit, CancellationToken cancellationToken);
    Task<ApiResponse<LeaderboardVersionScoresByPlayerIds>> GetLeaderboardVersionScoresByPlayerIdsAsync(string projectId, string environmentId, string leaderboardId, string versionId, List<string> playerIds, CancellationToken cancellationToken);
}
