using System.Net;
using Moq;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Api;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Client;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.UnitTest.Mock;

class LeaderboardApiV1AsyncMock
{
    public Mock<ILeaderboardsApiAsync> DefaultApiAsyncObject = new();

    public LeaderboardConfigPage ListResponse { get; } = new(
        new List<UpdatedLeaderboardConfig>());

    public ApiResponse<UpdatedLeaderboardConfig> GetResponse { get; set; } =
        new(statusCode: HttpStatusCode.Found, data: null!);

    public void SetUp()
    {
        DefaultApiAsyncObject.Reset();
        DefaultApiAsyncObject.Setup(a => a.Configuration)
            .Returns(new Gateway.LeaderboardApiV1.Generated.Client.Configuration());

        DefaultApiAsyncObject.Setup(
                a => a.GetLeaderboardConfigsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(ListResponse);

        DefaultApiAsyncObject.Setup(
                a => a.CreateLeaderboardWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<LeaderboardIdConfig>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<object>(statusCode: HttpStatusCode.Created, data: null!));

        DefaultApiAsyncObject.Setup(
                a => a.UpdateLeaderboardConfigWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<LeaderboardPatchConfig>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<object>(statusCode: HttpStatusCode.NoContent, data: null!));

        DefaultApiAsyncObject.Setup(
                a => a.GetLeaderboardConfigWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(GetResponse);

        DefaultApiAsyncObject.Setup(
                a => a.DeleteLeaderboardWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<object>(statusCode: HttpStatusCode.NoContent, data: null!));

        DefaultApiAsyncObject.Setup(
                a => a.ResetLeaderboardScoresWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    true,
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardVersionId>(statusCode: HttpStatusCode.OK, data: null!));

        DefaultApiAsyncObject.Setup(
                a => a.GetLeaderboardScoresWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardScoresPage>(
                statusCode: HttpStatusCode.OK,
                data: new LeaderboardScoresPage(0, 10, 0, new List<LeaderboardEntry>())));

        DefaultApiAsyncObject.Setup(
                a => a.GetLeaderboardPlayerScoreWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardEntryWithUpdatedTime>(
                statusCode: HttpStatusCode.OK,
                data: new LeaderboardEntryWithUpdatedTime(DateTime.UtcNow, Guid.Empty, "player1", "Player 1", 100.0, 1, null)));

        DefaultApiAsyncObject.Setup(
                a => a.GetLeaderboardScoresPlayerRangeWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardScores>(
                statusCode: HttpStatusCode.OK,
                data: new LeaderboardScores(new List<LeaderboardEntry>())));

        DefaultApiAsyncObject.Setup(
                a => a.GetLeaderboardScoresByTierWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardTierScoresPage>(
                statusCode: HttpStatusCode.OK,
                data: new LeaderboardTierScoresPage("tier1", 0, 10, 0, new List<LeaderboardEntry>())));

        DefaultApiAsyncObject.Setup(
                a => a.GetLeaderboardScoresByPlayerIdsWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<LeaderboardPlayerIds>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardScoresWithNotFoundPlayerIds>(
                statusCode: HttpStatusCode.OK,
                data: new LeaderboardScoresWithNotFoundPlayerIds(new List<LeaderboardEntry>(), new List<string>())));

        DefaultApiAsyncObject.Setup(
                a => a.DeleteLeaderboardPlayerScoreWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<object>(statusCode: HttpStatusCode.NoContent, data: null!));

        DefaultApiAsyncObject.Setup(
                a => a.DeleteLeaderboardPlayerScoreAllLeaderboardsWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<object>(statusCode: HttpStatusCode.NoContent, data: null!));

        DefaultApiAsyncObject.Setup(
                a => a.GetLeaderboardBucketsWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<BucketsPage>(
                statusCode: HttpStatusCode.OK,
                data: new BucketsPage(0, 10, 0, new List<Guid>())));

        DefaultApiAsyncObject.Setup(
                a => a.GetLeaderboardBucketScoresWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardScoresPage>(
                statusCode: HttpStatusCode.OK,
                data: new LeaderboardScoresPage(0, 10, 0, new List<LeaderboardEntry>())));

        DefaultApiAsyncObject.Setup(
                a => a.GetLeaderboardBucketScoresByTierWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardTierScoresPage>(
                statusCode: HttpStatusCode.OK,
                data: new LeaderboardTierScoresPage("tier1", 0, 10, 0, new List<LeaderboardEntry>())));

        DefaultApiAsyncObject.Setup(
                a => a.GetLeaderboardVersionScoresWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardVersionScoresPage>(
                statusCode: HttpStatusCode.OK,
                data: new LeaderboardVersionScoresPage(
                    new LeaderboardVersion("v1", DateTime.UtcNow, DateTime.UtcNow),
                    0, 10, 0, new List<LeaderboardEntry>())));

        DefaultApiAsyncObject.Setup(
                a => a.GetLeaderboardVersionScoresByTierWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardVersionTierScoresPage>(
                statusCode: HttpStatusCode.OK,
                data: new LeaderboardVersionTierScoresPage(
                    "tier1",
                    new LeaderboardVersion("v1", DateTime.UtcNow, DateTime.UtcNow),
                    0, 10, 0, new List<LeaderboardEntry>())));

        DefaultApiAsyncObject.Setup(
                a => a.GetLeaderboardVersionBucketsWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<VersionBucketsPage>(
                statusCode: HttpStatusCode.OK,
                data: new VersionBucketsPage(
                    0, 10, 0,
                    new LeaderboardVersion("v1", DateTime.UtcNow, DateTime.UtcNow),
                    new List<Guid>())));

        DefaultApiAsyncObject.Setup(
                a => a.GetLeaderboardVersionBucketScoresWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardVersionScoresPage>(
                statusCode: HttpStatusCode.OK,
                data: new LeaderboardVersionScoresPage(
                    new LeaderboardVersion("v1", DateTime.UtcNow, DateTime.UtcNow),
                    0, 10, 0, new List<LeaderboardEntry>())));

        DefaultApiAsyncObject.Setup(
                a => a.GetLeaderboardVersionBucketScoresByTierWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardVersionTierScoresPage>(
                statusCode: HttpStatusCode.OK,
                data: new LeaderboardVersionTierScoresPage(
                    "tier1",
                    new LeaderboardVersion("v1", DateTime.UtcNow, DateTime.UtcNow),
                    0, 10, 0, new List<LeaderboardEntry>())));

        DefaultApiAsyncObject.Setup(
                a => a.GetLeaderboardVersionPlayerScoreWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardVersionEntry>(
                statusCode: HttpStatusCode.OK,
                data: new LeaderboardVersionEntry(new LeaderboardVersion("v1", DateTime.UtcNow, DateTime.UtcNow), "player1", "Player 1", 100.0, 1, null)));

        DefaultApiAsyncObject.Setup(
                a => a.GetLeaderboardVersionScoresPlayerRangeWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardVersionRange>(
                statusCode: HttpStatusCode.OK,
                data: new LeaderboardVersionRange(
                    new LeaderboardVersion("v1", DateTime.UtcNow, DateTime.UtcNow),
                    new List<LeaderboardEntry>())));

        DefaultApiAsyncObject.Setup(
                a => a.GetLeaderboardScoresByPlayerIdsArchiveVersionWithHttpInfoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<LeaderboardPlayerIds>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardVersionScoresByPlayerIds>(
                statusCode: HttpStatusCode.OK,
                data: new LeaderboardVersionScoresByPlayerIds(
                    new LeaderboardVersion("v1", DateTime.UtcNow, DateTime.UtcNow),
                    new List<LeaderboardEntry>(),
                    new List<string>())));
    }
}
