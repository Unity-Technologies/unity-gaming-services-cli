using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Purchasing.Authoring;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Gateway.LiveContentApiV1.Generated.Api;
using Unity.Services.Gateway.LiveContentApiV1.Generated.Client;
using Unity.Services.Gateway.LiveContentApiV1.Generated.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;

namespace Unity.Services.Cli.Purchasing.UnitTest.Authoring;

[TestFixture]
class PurchasingClientTests
{
    const string k_EnvId = "env-id";
    const string k_ProjectId = "proj-id";
    const string k_Sku = "test";
    const string k_CatalogPath = "catalog/" + k_Sku;

    Mock<IConfigsApiAsync> m_ConfigsApi = null!;
    Mock<IServiceAccountAuthenticationService> m_Auth = null!;
    PurchasingClient m_Client = null!;

    static CatalogItem MakeItem() => new()
    {
        uSku = k_Sku,
        CatalogListingId = k_CatalogPath,
    };

    [SetUp]
    public async Task SetUp()
    {
        m_Auth = new Mock<IServiceAccountAuthenticationService>();
        m_Auth.Setup(a => a.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Bearer:mock-jwt");

        m_ConfigsApi = new Mock<IConfigsApiAsync>();

        var mockConfig = new Mock<Configuration>();
        var headers = new Dictionary<string, string>();
        mockConfig.Setup(c => c.DefaultHeaders).Returns(headers);
        m_ConfigsApi.Setup(a => a.Configuration).Returns(mockConfig.Object);

        m_Client = new PurchasingClient(m_ConfigsApi.Object, m_Auth.Object);
        await m_Client.Initialize(k_EnvId, k_ProjectId, CancellationToken.None);
    }

    [Test]
    public async Task Upsert_NewConfig_CallsCreateConfigFileAsync()
    {
        // GET returns 404 (config doesn't exist) → POST to create
        m_ConfigsApi
            .Setup(a => a.GetConfigContentAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<string>?>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(404, "Not Found"));

        m_ConfigsApi
            .Setup(a => a.CreateConfigFileAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<string>?>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LcmGetAssets200ResponseInner());

        await m_Client.Upsert(MakeItem(), CancellationToken.None);

        m_ConfigsApi.Verify(
            a => a.CreateConfigFileAsync(
                k_EnvId, k_ProjectId, k_CatalogPath,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<string>?>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);

        m_ConfigsApi.Verify(
            a => a.UpdateConfigFileAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<string>?>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Upsert_ExistingConfig_CallsUpdateConfigFileAsync()
    {
        // GET returns 200 (config exists) → PUT to update
        m_ConfigsApi
            .Setup(a => a.GetConfigContentAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<string>?>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, object> { ["uSKU"] = k_Sku });

        m_ConfigsApi
            .Setup(a => a.UpdateConfigFileAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<string>?>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LcmGetAssets200ResponseInner());

        await m_Client.Upsert(MakeItem(), CancellationToken.None);

        m_ConfigsApi.Verify(
            a => a.UpdateConfigFileAsync(
                k_EnvId, k_ProjectId, k_CatalogPath,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<string>?>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);

        m_ConfigsApi.Verify(
            a => a.CreateConfigFileAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<string>?>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Delete_CallsDeleteConfigAsync()
    {
        m_ConfigsApi
            .Setup(a => a.DeleteConfigAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<string>?>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LcmDeleteConfig200Response());

        await m_Client.Delete(MakeItem(), CancellationToken.None);

        m_ConfigsApi.Verify(
            a => a.DeleteConfigAsync(
                k_EnvId, k_ProjectId, k_CatalogPath,
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<string>?>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void Upsert_WhenApiThrowsNonNotFound_ThrowsClientException()
    {
        m_ConfigsApi
            .Setup(a => a.GetConfigContentAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<string>?>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(404, "Not Found"));

        m_ConfigsApi
            .Setup(a => a.CreateConfigFileAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<string>?>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(500, "Internal Server Error"));

        Assert.ThrowsAsync<ClientException>(
            () => m_Client.Upsert(MakeItem(), CancellationToken.None));
    }

    [Test]
    public void Delete_WhenApiThrows_ThrowsClientException()
    {
        m_ConfigsApi
            .Setup(a => a.DeleteConfigAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<string>?>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(404, "Not Found"));

        Assert.ThrowsAsync<ClientException>(
            () => m_Client.Delete(MakeItem(), CancellationToken.None));
    }
}
