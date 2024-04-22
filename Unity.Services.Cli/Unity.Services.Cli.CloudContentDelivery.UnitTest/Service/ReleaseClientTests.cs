using System.Net;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Api;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace CloudContentDeliveryTest.Service;

[TestFixture]
public class ReleaseClientTests
{
    readonly Mock<IServiceAccountAuthenticationService> m_AuthServiceMock = new();
    readonly Mock<IReleasesApi> m_ReleasesApiMock = new();
    readonly Mock<IContentDeliveryValidator> m_ContentDeliveryValidatorMock = new();
    ReleaseClient m_ReleaseClient = null!;
    const string k_TestAccessToken = "test-token";

    [SetUp]
    public void SetUp()
    {
        m_AuthServiceMock.Reset();
        m_ReleasesApiMock.Reset();
        m_ContentDeliveryValidatorMock.Reset();

        m_ReleasesApiMock.Setup(api => api.Configuration).Returns(new Configuration());
        m_AuthServiceMock.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .Returns(Task.FromResult(k_TestAccessToken));

        m_ReleaseClient = new ReleaseClient(
            m_AuthServiceMock.Object,
            m_ReleasesApiMock.Object,
            m_ContentDeliveryValidatorMock.Object);

        m_ReleasesApiMock.Setup(
                x => x.GetReleasesEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    1,
                    1,
                    CloudContentDeliveryTestsConstants.ReleaseNumber.ToString(),
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    0,
                    It.IsAny<CancellationToken>()
                ))
            .ReturnsAsync(
                new List<CcdGetBucket200ResponseLastRelease>
                {
                    new()
                    {
                        Releasenum = CloudContentDeliveryTestsConstants.ReleaseNumber,
                        Releaseid = new Guid(CloudContentDeliveryTestsConstants.ReleaseId)
                    }
                });

    }

    [Test]
    public async Task CreateReleaseAsync_ValidParameters_ReturnsRelease()
    {
        var expectedReleaseResponse = new CcdGetBucket200ResponseLastRelease();
        m_ReleasesApiMock.Setup(
                api => api.CreateReleaseEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    It.IsAny<CcdCreateReleaseRequest>(),
                    0,
                    CancellationToken.None))
            .ReturnsAsync(expectedReleaseResponse);
        var result = await m_ReleaseClient.CreateReleaseAsync(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            It.IsAny<CcdCreateReleaseRequest>(),
            CancellationToken.None);
        Assert.That(result, Is.EqualTo(expectedReleaseResponse));
    }

    [Test]
    public async Task UpdateReleaseAsync_ValidParameters_ReturnsRelease()
    {
        var expectedReleaseResponse = new CcdGetBucket200ResponseLastRelease();

        m_ReleasesApiMock.Setup(
                api => api.UpdateReleaseEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.ReleaseId,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    It.IsAny<CcdUpdateReleaseRequest>(),
                    0,
                    CancellationToken.None))
            .ReturnsAsync(expectedReleaseResponse);
        var result = await m_ReleaseClient.UpdateReleaseAsync(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            CloudContentDeliveryTestsConstants.ReleaseNumber,
            CloudContentDeliveryTestsConstants.Notes,
            CancellationToken.None);
        Assert.That(result, Is.EqualTo(expectedReleaseResponse));
    }

    [Test]
    public async Task GetReleaseAsync_ValidParameters_ReturnsRelease()
    {
        var expectedReleaseResponse = new CcdGetBucket200ResponseLastRelease();

        m_ReleasesApiMock.Setup(
                api => api.GetReleaseEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.ReleaseId,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    0,
                    CancellationToken.None))
            .ReturnsAsync(expectedReleaseResponse);
        var result = await m_ReleaseClient.GetReleaseAsync(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            CloudContentDeliveryTestsConstants.ReleaseNumber,
            CancellationToken.None);
        Assert.That(result, Is.EqualTo(expectedReleaseResponse));
    }

    [Test]
    public async Task GetReleaseByBadgeNameAsync_ValidParameters_ReturnsRelease()
    {
        var expectedReleaseResponse = new CcdGetBucket200ResponseLastRelease();
        m_ReleasesApiMock.Setup(
                api => api.GetReleaseByBadgeEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.BadgeName,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    0,
                    CancellationToken.None))
            .ReturnsAsync(expectedReleaseResponse);
        var result = await m_ReleaseClient.GetReleaseByBadgeNameAsync(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            CloudContentDeliveryTestsConstants.BadgeName,
            CancellationToken.None);
        Assert.That(result, Is.EqualTo(expectedReleaseResponse));
    }

    [Test]
    public async Task ListReleaseAsync_ValidParameters_ReturnsReleases()
    {
        var expectedReleaseList = new ApiResponse<List<CcdGetBucket200ResponseLastRelease>>(
            HttpStatusCode.OK,
            new Multimap<string, string>(),
            new List<CcdGetBucket200ResponseLastRelease>
            {
                new(),
                new()
            }
        );

        m_ReleasesApiMock.Setup(
                api => api.GetReleasesEnvWithHttpInfoAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.Page,
                    CloudContentDeliveryTestsConstants.PerPage,
                    CloudContentDeliveryTestsConstants.ReleaseNumber.ToString(),
                    CloudContentDeliveryTestsConstants.Notes,
                    CloudContentDeliveryTestsConstants.PromotedFromBucket,
                    CloudContentDeliveryTestsConstants.PromotedFromRelease,
                    CloudContentDeliveryTestsConstants.BadgeName,
                    CloudContentDeliveryTestsConstants.SortBy,
                    CloudContentDeliveryTestsConstants.SortOrder,
                    0,
                    CancellationToken.None))
            .ReturnsAsync(expectedReleaseList);

        var result = await m_ReleaseClient.ListReleaseAsync(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketId,
            CloudContentDeliveryTestsConstants.Page,
            CloudContentDeliveryTestsConstants.PerPage,
            CloudContentDeliveryTestsConstants.ReleaseNumber.ToString(),
            CloudContentDeliveryTestsConstants.PromotedFromBucket,
            CloudContentDeliveryTestsConstants.PromotedFromRelease,
            CloudContentDeliveryTestsConstants.BadgeName,
            CloudContentDeliveryTestsConstants.Notes,
            CloudContentDeliveryTestsConstants.SortBy,
            CloudContentDeliveryTestsConstants.SortOrder,
            CancellationToken.None);

        Assert.IsNotNull(result.Data);
        Assert.That(result.Data.Count, Is.EqualTo(expectedReleaseList.Data.Count));
    }

    [Test]
    public async Task AuthorizeServiceAsync()
    {
        await m_ReleaseClient.AuthorizeServiceAsync(CancellationToken.None);
        m_AuthServiceMock.Verify(a => a.GetAccessTokenAsync(CancellationToken.None), Times.Once);
        Assert.Multiple(
            () =>
            {
                Assert.That(
                    m_ReleasesApiMock.Object.Configuration.DefaultHeaders["Authorization"],
                    Is.EqualTo($"Basic {k_TestAccessToken}"));
            });
    }
}
