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
    const string leaderboardId = "leaderboard_id";
    const bool archive = true;

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

        m_ExpectedLeaderboard = new(id: leaderboardId,
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

        var config = new LeaderboardIdConfig(id: leaderboardId, name: "lb_name_1", sortOrder: SortOrder.Asc,
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
                    TestValues.ValidProjectId, TestValues.ValidEnvironmentId, leaderboardId,
                    "{\"id\": \"lb1\", \"name\": \"lb_name_1\", \"sortOrder\": \"asc\", \"updateType\": \"aggregate\", \"bucketSize\": 10}",
                    CancellationToken.None));

        var config = new LeaderboardPatchConfig(name: "lb_name_1", sortOrder: SortOrder.Asc,
            updateType: UpdateType.Aggregate);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            ex => ex.UpdateLeaderboardConfigWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                leaderboardId,
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
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, leaderboardId, CancellationToken.None);

        Assert.AreEqual(m_ExpectedLeaderboard, actualLeaderboard.Data);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLeaderboardConfigWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                leaderboardId,
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
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, leaderboardId, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.DeleteLeaderboardWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                leaderboardId,
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
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, leaderboardId, archive, CancellationToken.None);

        m_LeaderboardApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.ResetLeaderboardScoresWithHttpInfoAsync(
                Guid.Parse(TestValues.ValidProjectId),
                Guid.Parse(TestValues.ValidEnvironmentId),
                leaderboardId,
                archive,
                0,
                CancellationToken.None),
            Times.Once);
    }
}
