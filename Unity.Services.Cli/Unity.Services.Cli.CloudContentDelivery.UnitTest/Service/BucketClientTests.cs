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
public class BucketClientTests
{

    readonly Mock<IServiceAccountAuthenticationService> m_AuthServiceMock = new();
    readonly Mock<IBucketsApi> m_BucketsApiMock = new();
    readonly Mock<IPermissionsApi> m_PermissionsApiMock = new();

    readonly Mock<IContentDeliveryValidator> m_ContentDeliveryValidator = new();
    BucketClient m_BucketClient = null!;

    const string k_TestAccessToken = "test-token";

    [SetUp]
    public void SetUp()
    {
        m_AuthServiceMock.Reset();
        m_BucketsApiMock.Reset();
        m_PermissionsApiMock.Reset();
        m_ContentDeliveryValidator.Reset();
        m_BucketsApiMock.Setup(api => api.Configuration).Returns(new Configuration());
        m_PermissionsApiMock.Setup(api => api.Configuration).Returns(new Configuration());

        m_BucketsApiMock.Setup(
                x => x.ListBucketsByProjectEnvWithHttpInfoAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    CloudContentDeliveryTestsConstants.BucketName,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                ))
            .ReturnsAsync(
                new ApiResponse<List<CcdGetBucket200Response>>(
                    HttpStatusCode.OK,
                    new Multimap<string, string>(),
                    new List<CcdGetBucket200Response>
                    {new()
                    {
                        Name = "NewBucket",
                        Id = new Guid(CloudContentDeliveryTestsConstants.BucketId)
                    }}
                ));

        m_BucketsApiMock.Setup(
                x => x.ListBucketsByProjectEnvAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    "missing-bucket",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                ))
            .ReturnsAsync(
                new List<CcdGetBucket200Response>(
                ));

        m_AuthServiceMock.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .Returns(Task.FromResult(k_TestAccessToken));

        m_BucketClient = new BucketClient(
            m_AuthServiceMock.Object,
            m_BucketsApiMock.Object,
            m_PermissionsApiMock.Object,
            m_ContentDeliveryValidator.Object);
    }

    [Test]
    public async Task AuthorizeServiceAsync()
    {
        await m_BucketClient.AuthorizeServiceAsync(CancellationToken.None);
        m_AuthServiceMock.Verify(a => a.GetAccessTokenAsync(CancellationToken.None), Times.Once);
        Assert.Multiple(
            () =>
            {
                Assert.That(
                    m_BucketsApiMock.Object.Configuration.DefaultHeaders["Authorization"],
                    Is.EqualTo($"Basic {k_TestAccessToken}"));
                Assert.That(
                    m_PermissionsApiMock.Object.Configuration.DefaultHeaders["Authorization"],
                    Is.EqualTo($"Basic {k_TestAccessToken}"));
            });
    }

    [Test]
    public void GetBucketAsync_AuthenticationFails_ThrowsException()
    {
        m_AuthServiceMock.Reset();
        m_AuthServiceMock.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .ThrowsAsync(new Exception("Authentication failed"));

        Assert.ThrowsAsync<Exception>(
            async () =>
            {
                await m_BucketClient.GetBucketAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId);
            });
    }

    [Test]
    public void GetBucketAsync_InvalidProjectIdException()
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
                await m_BucketClient.GetBucketAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId);
            });

        Assert.That(exception?.Message, Is.EqualTo("Your project-id is not valid. "));
        m_ContentDeliveryValidator.Verify(
            v => v.ValidateProjectIdAndEnvironmentId(
                CloudContentDeliveryTestsConstants.ProjectId,
                CloudContentDeliveryTestsConstants.EnvironmentId),
            Times.Once);
    }

    [Test]
    public void GetBucketAsync_InvalidEnvironmentIdException()
    {
        m_ContentDeliveryValidator.Setup(
                v => v.ValidateProjectIdAndEnvironmentId(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.EnvironmentId, "", It.IsAny<string>()));
        var exception = Assert.ThrowsAsync<ConfigValidationException>(
            async () =>
            {
                await m_BucketClient.GetBucketAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId);
            });

        Assert.That(exception?.Message, Is.EqualTo("Your environment-id is not valid. "));

    }

    [Test]
    public async Task GetBucketAsync_BucketExists_ReturnsValidBucket()
    {
        var expectedBucketResponse = new CcdGetBucket200Response
        {
            Id = new Guid(CloudContentDeliveryTestsConstants.BucketId),
            Projectguid = new Guid(CloudContentDeliveryTestsConstants.ProjectId)
        };

        m_BucketsApiMock.Setup(
                api => api.GetBucketEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketId,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    0,
                    CancellationToken.None))
            .ReturnsAsync(expectedBucketResponse);

        var result = await m_BucketClient.GetBucketAsync(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketName);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(
            () =>
            {
                Assert.That(result!.Projectguid, Is.EqualTo(expectedBucketResponse.Projectguid));
                Assert.That(result.Id, Is.EqualTo(expectedBucketResponse.Id));
            });
    }

    [Test]
    public void GetBucketAsync_BucketDoesNotExist_ThrowsException()
    {
        var bucketId = "missing-bucket";
        m_BucketsApiMock.Setup(
                api => api.GetBucketEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    bucketId,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    0,
                    CancellationToken.None))
            .ThrowsAsync(new Exception("Bucket not found"));

        Assert.ThrowsAsync<CliException>(
            async () =>
            {
                await m_BucketClient.GetBucketAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    bucketId);
            });
    }

    [Test]
    public async Task DeleteBucketAsync_BucketExists_ReturnsDeletedMessage()
    {
        var result = await m_BucketClient.DeleteBucketAsync(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketName);
        Assert.That(result, Is.EqualTo("Bucket deleted."));
    }

    [Test]
    public void DeleteBucketAsync_BucketDoesNotExist_ThrowsException()
    {
        var bucketId = "missing-bucket";

        Assert.ThrowsAsync<CliException>(
            async () =>
            {
                await m_BucketClient.DeleteBucketAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    bucketId);
            });
    }

    [Test]
    public async Task ListBucketAsync_ValidParameters_ReturnsBuckets()
    {
        var expectedBucketList = new ApiResponse<List<CcdGetBucket200Response>>(
            HttpStatusCode.OK,
            new Multimap<string, string>(),
            new List<CcdGetBucket200Response>
            {
                new(),
                new()
            }
        );

        m_BucketsApiMock.Setup(
                api => api.ListBucketsByProjectEnvWithHttpInfoAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.Page,
                    CloudContentDeliveryTestsConstants.PerPage,
                    CloudContentDeliveryTestsConstants.BucketName,
                    CloudContentDeliveryTestsConstants.BucketDescription,
                    CloudContentDeliveryTestsConstants.SortBy,
                    CloudContentDeliveryTestsConstants.SortOrder,
                    default,
                    CancellationToken.None))
            .ReturnsAsync(expectedBucketList);

        var result = await m_BucketClient.ListBucketAsync(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.Page,
            CloudContentDeliveryTestsConstants.PerPage,
            CloudContentDeliveryTestsConstants.BucketName,
            CloudContentDeliveryTestsConstants.BucketDescription,
            CloudContentDeliveryTestsConstants.SortBy,
            CloudContentDeliveryTestsConstants.SortOrder,
            CancellationToken.None);

        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data, Has.Count.EqualTo(expectedBucketList.Data.Count));
    }

    [Test]
    public async Task CreateBucketAsync_ValidParameters_ReturnsBucketResult()
    {
        var expectedBucketResult = new CcdGetBucket200Response();

        m_BucketsApiMock.Setup(
                api => api.CreateBucketByProjectEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    It.IsAny<CcdCreateBucketByProjectRequest>(),
                    0,
                    CancellationToken.None))
            .ReturnsAsync(new CcdGetBucket200Response());

        var result = await m_BucketClient.CreateBucketAsync(
            CloudContentDeliveryTestsConstants.ProjectId,
            CloudContentDeliveryTestsConstants.EnvironmentId,
            CloudContentDeliveryTestsConstants.BucketName,
            CloudContentDeliveryTestsConstants.BucketDescription,
            true);

        Assert.IsNotNull(result);

        Assert.That(result, Is.EqualTo(expectedBucketResult));
    }

    [Test]
    public void CreateBucketAsync_InvalidParameters_ThrowsSpecificException()
    {
        m_BucketsApiMock.Setup(
                api => api.CreateBucketByProjectEnvAsync(
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.ProjectId,
                    It.IsAny<CcdCreateBucketByProjectRequest>(),
                    0,
                    CancellationToken.None))
            .ThrowsAsync(new Exception("Invalid parameters"));

        Assert.ThrowsAsync<Exception>(
            async () =>
            {
                await m_BucketClient.CreateBucketAsync(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId,
                    CloudContentDeliveryTestsConstants.BucketName,
                    "",
                    true);
            });
    }

}
