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
    }
}
