using Unity.Services.Gateway.LeaderboardApiV1.Generated.Client;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.Service;

public interface ILeaderboardsService
{
    public Task<IEnumerable<UpdatedLeaderboardConfig>> GetLeaderboardsAsync(string projectId, string environmentId,
        string? cursor, int? limit, CancellationToken cancellationToken = default);

    Task<ApiResponse<UpdatedLeaderboardConfig>> GetLeaderboardAsync(string projectId, string environmentId, string leaderboardId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> CreateLeaderboardAsync(string projectId, string environmentId, string body,
        CancellationToken cancellationToken);

    Task<ApiResponse<object>> UpdateLeaderboardAsync(string projectId, string environmentId, string leaderboardId,
        string body, CancellationToken cancellationToken);

    Task<ApiResponse<object>> DeleteLeaderboardAsync(string projectId, string environmentId, string leaderboardId,
        CancellationToken cancellationToken);

    Task<ApiResponse<LeaderboardVersionId>> ResetLeaderboardAsync(string projectId, string environmentId,
        string leaderboardId, bool? archive, CancellationToken cancellationToken);

    T DeserializeBody<T>(string value);
}
