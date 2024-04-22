using System.Net;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Api;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;
using Configuration = Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client.Configuration;

namespace CloudContentDeliveryTest.Service;

[TestFixture]
public class BadgeClientTests
{
    readonly Mock<IServiceAccountAuthenticationService> m_AuthServiceMock = new();
    readonly Mock<IBadgesApi> m_BadgesApiMock = new();
    readonly Mock<IContentDeliveryValidator> m_ContentDeliveryValidator = new();
    readonly Mock<HttpClient> m_HttpClient = new();
    BadgeClient m_BadgeClient = null!;
    const string k_TestAccessToken = "test-token";

    [SetUp]
    public void SetUp()
    {
        m_AuthServiceMock.Reset();
        m_BadgesApiMock.Reset();
        m_HttpClient.Reset();
        m_ContentDeliveryValidator.Reset();
        m_BadgesApiMock.Setup(api => api.Configuration).Returns(new Configuration());

        m_AuthServiceMock.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .Returns(Task.FromResult(k_TestAccessToken));

        m_ContentDeliveryValidator.Setup(
            v => v.ValidateProjectIdAndEnvironmentId(It.IsAny<string>(), It.IsAny<string>()));

        m_BadgeClient = new BadgeClient(
            m_AuthServiceMock.Object,
            m_BadgesApiMock.Object,
            m_ContentDeliveryValidator.Object);
    }

    [Test]
    public async Task AuthorizeServiceAsync()
    {
        await m_BadgeClient.AuthorizeServiceAsync(CancellationToken.None);
        m_AuthServiceMock.Verify(a => a.GetAccessTokenAsync(CancellationToken.None), Times.Once);
        Assert.Multiple(
            () =>
            {
                Assert.That(
                    m_BadgesApiMock.Object.Configuration.DefaultHeaders["Authorization"],
                    Is.EqualTo($"Basic {k_TestAccessToken}"));
            });
    }

    [Test]
    public void DeleteBadgeAsync_AuthenticationFails_ThrowsException()
    {
        m_AuthServiceMock.Reset();
        m_AuthServiceMock.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .ThrowsAsync(new Exception("Authentication failed"));

        Assert.ThrowsAsync<Exception>(
            async () =>
            {
                await m_BadgeClient.DeleteBadgeAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.BadgeName,
                    CancellationToken.None);
            });
    }

    [Test]
    public void DeleteBadgeAsync_InvalidProjectIdException()
    {
        m_ContentDeliveryValidator
            .Setup(
                v => v.ValidateProjectIdAndEnvironmentId(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, "", It.IsAny<string>()));

        var exception = Assert.ThrowsAsync<ConfigValidationException>(
            async () =>
            {
                await m_BadgeClient.DeleteBadgeAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.BadgeName,
                    CancellationToken.None);
            });

        Assert.That(exception?.Message, Is.EqualTo("Your project-id is not valid. "));
        m_ContentDeliveryValidator.Verify(
            v => v.ValidateProjectIdAndEnvironmentId(
                CloudContentDeliveryTestsConstants.ProjectId,
                CloudContentDeliveryTestsConstants.EnvironmentId),
            Times.Once);
    }

    [Test]
    public void DeleteBadgeAsync_InvalidEnvironmentIdException()
    {
        m_ContentDeliveryValidator.Setup(
                v => v.ValidateProjectIdAndEnvironmentId(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.EnvironmentId, "", It.IsAny<string>()));
        var exception = Assert.ThrowsAsync<ConfigValidationException>(
            async () =>
            {
                await m_BadgeClient.DeleteBadgeAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.BadgeName,
                    CancellationToken.None);
            });

        Assert.That(exception?.Message, Is.EqualTo("Your environment-id is not valid. "));

    }

    [Test]
    public async Task ListBadgeAsync_ValidParameters_ReturnsBadges()
    {

        var expectedBadgeList = new ApiResponse<List<CcdGetBucket200ResponseLastReleaseBadgesInner>>(
            HttpStatusCode.OK,
            new Multimap<string, string>(),
            new List<CcdGetBucket200ResponseLastReleaseBadgesInner>
            {
                new(),
                new()
            }
        );

        m_BadgesApiMock.Setup(
                api => api.ListBadgesEnvWithHttpInfoAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.Page,
                    CloudContentDeliveryTestsConstants.PerPage,
                    CloudContentDeliveryTestsConstants.BadgeName,
                    CloudContentDeliveryTestsConstants.ReleaseNumber.ToString(),
                    CloudContentDeliveryTestsConstants.SortBy,
                    CloudContentDeliveryTestsConstants.SortOrder,
                    0,
                    CancellationToken.None))
            .ReturnsAsync(expectedBadgeList);

        var result = await m_BadgeClient.ListBadgeAsync(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            CloudContentDeliveryTestsConstants.Page,
            CloudContentDeliveryTestsConstants.PerPage,
            CloudContentDeliveryTestsConstants.BadgeName,
            CloudContentDeliveryTestsConstants.ReleaseNumber.ToString(),
            CloudContentDeliveryTestsConstants.SortBy,
            CloudContentDeliveryTestsConstants.SortOrder,
            CancellationToken.None);

        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data, Has.Count.EqualTo(expectedBadgeList.Data.Count));
    }

    [Test]
    public async Task AddBadgeAsync_ValidParameters_ReturnsBadge()
    {
        var expectedBadgeResponse = new CcdGetBucket200ResponseLastReleaseBadgesInner();

        m_BadgesApiMock.Setup(
                api => api.UpdateBadgeEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    It.IsAny<CcdUpdateBadgeRequest>(),
                    0,
                    CancellationToken.None))
            .ReturnsAsync(expectedBadgeResponse);

        var result = await m_BadgeClient.CreateBadgeAsync(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            CloudContentDeliveryTestsConstants.BadgeName,
            CloudContentDeliveryTestsConstants.ReleaseNumber,
            CancellationToken.None);

        Assert.That(result, Is.EqualTo(expectedBadgeResponse));
    }

    [Test]
    public async Task DeleteBadgeAsync_ValidParameters_ReturnsDeletedMessage()
    {
        var result = await m_BadgeClient.DeleteBadgeAsync(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            CloudContentDeliveryTestsConstants.BadgeName,
            CancellationToken.None);
        Assert.That(result, Is.EqualTo("Badge deleted."));
    }

}
