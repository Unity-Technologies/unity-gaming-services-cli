using System.Net;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Leaderboards.Service;
using Unity.Services.Cli.Leaderboards.UnitTest.Mock;
using Unity.Services.Cli.Leaderboards.UnitTest.Utils;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Client;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.UnitTest.Service;

[TestFixture]
class LeaderboardsServiceTests
{
    const string k_TestAccessToken = "test-token";
    const string k_InvalidProjectId = "invalidProject";
    const string k_InvalidEnvironmentId = "foo";
    const string k_LeaderboardId = "leaderboard_id";
    const bool k_Archive = true;
    const string k_PlayerId = "player1";
    const string k_VersionId = "version1";
    const string k_TierId = "tier1";
    const string k_BucketId = "00000000-0000-0000-0000-000000000001";

    readonly Mock<IConfigurationValidator> m_ValidatorObject = new();
    readonly Mock<IServiceAccountAuthenticationService> m_AuthenticationServiceObject = new();
    readonly LeaderboardApiV1AsyncMock m_LeaderboardApiV1AsyncMock = new();

    LeaderboardsService? m_LeaderboardsService;
    List<UpdatedLeaderboardConfig>? m_ExpectedLeaderboards;
    UpdatedLeaderboardConfig? m_ExpectedLeaderboard;

    [SetUp]
    public void SetUp()
    {
        m_ValidatorObject.Reset();
        m_AuthenticationServiceObject.Reset();
        m_AuthenticationServiceObject.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .Returns(Task.FromResult(k_TestAccessToken));

        m_ExpectedLeaderboard = new(id: k_LeaderboardId,
            name: "leaderboard_name");

        m_ExpectedLeaderboards = new List<UpdatedLeaderboardConfig>
        {
            m_ExpectedLeaderboard
        };
        m_LeaderboardApiV1AsyncMock.GetResponse =
            new ApiResponse<UpdatedLeaderboardConfig>(statusCode: HttpStatusCode.Found, data: m_ExpectedLeaderboard);
        m_LeaderboardApiV1AsyncMock.ListResponse.Results = m_ExpectedLeaderboards;
        m_LeaderboardApiV1AsyncMock.SetUp();

        m_LeaderboardsService = new LeaderboardsService(
            m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Object,
            m_ValidatorObject.Object,
            m_AuthenticationServiceObject.Object);
    }

    [Test]
    public async Task AuthorizeLeaderboardService()
    {
        await m_LeaderboardsService!.AuthorizeServiceAsync(CancellationToken.None);
        m_AuthenticationServiceObject.Verify(a => a.GetAccessTokenAsync(CancellationToken.None));
        Assert.AreEqual(
            k_TestAccessToken.ToHeaderValue(),
            m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Object.Configuration.DefaultHeaders[
                AccessTokenHelper.HeaderKey]);
    }

    [Test]
    public async Task ListAsync_EmptyListSuccess()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);
        m_ExpectedLeaderboards!.Clear();

        var actualScripts = await m_LeaderboardsService!.GetLeaderboardsAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, null,null, CancellationToken.None);

        Assert.AreEqual(0, actualScripts.Count());
    }

    [Test]
    public async Task ListAsync_ValidParamsGetExpectedLeaderboardList()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var actualLeaderboards = await m_LeaderboardsService!.GetLeaderboardsAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, null,null, CancellationToken.None);

        CollectionAssert.AreEqual(m_ExpectedLeaderboards, actualLeaderboards);
        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardConfigsAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public void InvalidProjectIdThrowConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId, It.IsAny<string>()));
        Assert.Throws<ConfigValidationException>(
            () => m_LeaderboardsService!.ValidateProjectIdAndEnvironmentId(
                k_InvalidProjectId, TestValues.ValidEnvironmentId));
    }

    [Test]
    public void InvalidEnvironmentIdThrowConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId, It.IsAny<string>()));
        Assert.Throws<ConfigValidationException>(
            () => m_LeaderboardsService!.ValidateProjectIdAndEnvironmentId(
                TestValues.ValidProjectId, k_InvalidEnvironmentId));
    }

    [Test]
    public void CreateAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        Assert.DoesNotThrowAsync(
            () =>
             m_LeaderboardsService!.CreateLeaderboardAsync(
                TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
                "{\"id\": \"leaderboard_id\", \"name\": \"lb_name_1\", \"sortOrder\": \"asc\", \"updateType\": \"aggregate\", \"bucketSize\": 10}",
                CancellationToken.None));

        var config = new LeaderboardIdConfig(id: k_LeaderboardId, name: "lb_name_1", sortOrder: SortOrder.Asc,
            updateType: UpdateType.Aggregate, bucketSize: 10);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            ex => ex.CreateLeaderboardWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                config,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public void CreateAsync_FailedWithDeserializeBody()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        Assert.ThrowsAsync<CliException>(
            () =>
                m_LeaderboardsService!.CreateLeaderboardAsync(
                    TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
                    "{",
                    CancellationToken.None));
    }

    [Test]
    public void UpdateAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        Assert.DoesNotThrowAsync(
            () =>
                m_LeaderboardsService!.UpdateLeaderboardAsync(
                    TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId,
                    "{\"id\": \"lb1\", \"name\": \"lb_name_1\", \"sortOrder\": \"asc\", \"updateType\": \"aggregate\", \"bucketSize\": 10}",
                    CancellationToken.None));

        var config = new LeaderboardPatchConfig(name: "lb_name_1", sortOrder: SortOrder.Asc,
            updateType: UpdateType.Aggregate);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            ex => ex.UpdateLeaderboardConfigWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                config,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetAsync_LeaderboardSucceeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var actualLeaderboard = await m_LeaderboardsService!.GetLeaderboardAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, CancellationToken.None);

        Assert.AreEqual(m_ExpectedLeaderboard, actualLeaderboard.Data);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardConfigWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task DeleteAsync_LeaderboardSucceeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_LeaderboardsService!.DeleteLeaderboardAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.DeleteLeaderboardWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task ResetAsync_LeaderboardSucceeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_LeaderboardsService!.ResetLeaderboardAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, k_Archive, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.ResetLeaderboardScoresWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                k_Archive,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetLeaderboardScoresAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_LeaderboardsService!.GetLeaderboardScoresAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, null, null, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardScoresWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                null,
                null,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetLeaderboardPlayerScoreAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_LeaderboardsService!.GetLeaderboardPlayerScoreAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, k_PlayerId, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardPlayerScoreWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                k_PlayerId,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetLeaderboardScoresPlayerRangeAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_LeaderboardsService!.GetLeaderboardScoresPlayerRangeAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, k_PlayerId, null, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardScoresPlayerRangeWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                k_PlayerId,
                null,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetLeaderboardScoresByTierAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_LeaderboardsService!.GetLeaderboardScoresByTierAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, k_TierId, null, null, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardScoresByTierWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                k_TierId,
                null,
                null,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetLeaderboardScoresByPlayerIdsAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var playerIds = new List<string> { k_PlayerId };
        await m_LeaderboardsService!.GetLeaderboardScoresByPlayerIdsAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, playerIds, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardScoresByPlayerIdsWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                It.IsAny<LeaderboardPlayerIds>(),
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task DeleteLeaderboardPlayerScoreAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_LeaderboardsService!.DeleteLeaderboardPlayerScoreAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, k_PlayerId, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.DeleteLeaderboardPlayerScoreWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                k_PlayerId,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task PurgeLeaderboardPlayerScoresAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_LeaderboardsService!.PurgeLeaderboardPlayerScoresAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_PlayerId, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.DeleteLeaderboardPlayerScoreAllLeaderboardsWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_PlayerId,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetLeaderboardBucketsAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_LeaderboardsService!.GetLeaderboardBucketsAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, null, null, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardBucketsWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                null,
                null,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetLeaderboardBucketScoresAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_LeaderboardsService!.GetLeaderboardBucketScoresAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, k_BucketId, null, null, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardBucketScoresWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                Guid.Parse(k_BucketId),
                null,
                null,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetLeaderboardBucketScoresByTierAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_LeaderboardsService!.GetLeaderboardBucketScoresByTierAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, k_BucketId, k_TierId, null, null, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardBucketScoresByTierWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                Guid.Parse(k_BucketId),
                k_TierId,
                null,
                null,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetLeaderboardVersionScoresAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_LeaderboardsService!.GetLeaderboardVersionScoresAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, k_VersionId, null, null, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardVersionScoresWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                k_VersionId,
                null,
                null,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetLeaderboardVersionScoresByTierAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_LeaderboardsService!.GetLeaderboardVersionScoresByTierAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, k_VersionId, k_TierId, null, null, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardVersionScoresByTierWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                k_VersionId,
                k_TierId,
                null,
                null,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetLeaderboardVersionBucketsAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_LeaderboardsService!.GetLeaderboardVersionBucketsAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, k_VersionId, null, null, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardVersionBucketsWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                k_VersionId,
                null,
                null,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetLeaderboardVersionBucketScoresAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_LeaderboardsService!.GetLeaderboardVersionBucketScoresAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, k_VersionId, k_BucketId, null, null, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardVersionBucketScoresWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                k_VersionId,
                Guid.Parse(k_BucketId),
                null,
                null,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetLeaderboardVersionBucketScoresByTierAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_LeaderboardsService!.GetLeaderboardVersionBucketScoresByTierAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, k_VersionId, k_BucketId, k_TierId, null, null, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardVersionBucketScoresByTierWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                k_VersionId,
                Guid.Parse(k_BucketId),
                k_TierId,
                null,
                null,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetLeaderboardVersionPlayerScoreAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_LeaderboardsService!.GetLeaderboardVersionPlayerScoreAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, k_VersionId, k_PlayerId, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardVersionPlayerScoreWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                k_VersionId,
                k_PlayerId,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetLeaderboardVersionScoresPlayerRangeAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_LeaderboardsService!.GetLeaderboardVersionScoresPlayerRangeAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, k_VersionId, k_PlayerId, null, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardVersionScoresPlayerRangeWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                k_VersionId,
                k_PlayerId,
                null,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetLeaderboardVersionScoresByPlayerIdsAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var playerIds = new List<string> { k_PlayerId };
        await m_LeaderboardsService!.GetLeaderboardVersionScoresByPlayerIdsAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_LeaderboardId, k_VersionId, playerIds, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardScoresByPlayerIdsArchiveVersionWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                k_LeaderboardId,
                k_VersionId,
                It.IsAny<LeaderboardPlayerIds>(),
                0,
                CancellationToken.None),
            Times.Once);
    }
}
