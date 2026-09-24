using System.Net;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Services.Cli.Purchasing.Authoring;
using Unity.Services.Cli.Purchasing.Exceptions;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.LiveContentApiV1.Generated.Api;
using Unity.Services.Gateway.LiveContentApiV1.Generated.Client;
using Unity.Services.Gateway.LiveContentApiV1.Generated.Model;

namespace Unity.Services.Cli.Purchasing.UnitTest.Authoring;

[TestFixture]
class CliLiveContentApiTransportTests
{
    const string k_EnvironmentId = "env-id";
    const string k_ProjectId = "project-id";
    const string k_Path = "catalog/test";
    const string k_Token = "Bearer:mock-jwt";

    Mock<IConfigsApiAsync> m_ConfigsApi = null!;
    Mock<IServiceAccountAuthenticationService> m_Auth = null!;
    CliLiveContentApiTransport m_Transport = null!;
    Dictionary<string, string> m_Headers = null!;

    [SetUp]
    public async Task SetUp()
    {
        m_Auth = new Mock<IServiceAccountAuthenticationService>();
        m_Auth.Setup(auth => auth.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(k_Token);

        m_Headers = new Dictionary<string, string>();
        var configuration = new Mock<Configuration>();
        configuration.Setup(config => config.DefaultHeaders).Returns(m_Headers);

        m_ConfigsApi = new Mock<IConfigsApiAsync>();
        m_ConfigsApi.Setup(api => api.Configuration).Returns(configuration.Object);

        m_Transport = new CliLiveContentApiTransport(m_ConfigsApi.Object, m_Auth.Object);
        await m_Transport.InitializeAsync(k_EnvironmentId, k_ProjectId, CancellationToken.None);
    }

    [Test]
    public void InitializeAsync_SetsAuthorizationHeader()
    {
        Assert.That(m_Headers[AccessTokenHelper.HeaderKey], Is.EqualTo(k_Token.ToHeaderValue()));
    }

    [Test]
    public async Task GetConfigsAsync_SuccessFlattensVariantsAndPreservesHeaders()
    {
        var response = new ApiResponse<List<LcmGetAssets200ResponseInnerOneOf1>>(
            HttpStatusCode.OK,
            Headers(("X-Next-Cursor", "next-page"), ("Retry-After", "3")),
            new List<LcmGetAssets200ResponseInnerOneOf1> { GroupedConfig() });
        m_ConfigsApi.Setup(api => api.GetConfigsWithHttpInfoAsync(
                k_EnvironmentId, k_ProjectId, null, "cursor", false, 25, "catalog/", "schema",
                null, It.Is<List<string>>(tags => IsTagless(tags)), null, null, null, null, null, null, null, null,
                0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await m_Transport.GetConfigsAsync(
            "catalog/", 25, "cursor", false, "schema", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Headers["X-Next-Cursor"], Is.EqualTo("next-page"));
            Assert.That(result.Headers["Retry-After"], Is.EqualTo("3"));
            Assert.That(result.Content, Has.Count.EqualTo(2));
            Assert.That(result.Content[0].Id, Is.EqualTo("config-id"));
            Assert.That(result.Content[0].Path, Is.EqualTo(k_Path));
            Assert.That(result.Content[0].SortIndex, Is.EqualTo(7));
            Assert.That(result.Content[0].Schemas, Is.EqualTo(new[] { "schema" }));
            Assert.That(result.Content[0].VariantTags, Is.Empty);
            Assert.That(result.Content[0].ContentHash, Is.EqualTo("hash-a"));
            Assert.That(result.Content[0].Metadata["owner"], Is.EqualTo("iap"));
            Assert.That(result.Content[1].VariantTags, Is.EqualTo(new[] { "beta" }));
        });
    }

    [Test]
    public async Task GetConfigsAsync_FailureReturnsErrorWithoutMappingBody()
    {
        var response = new ApiResponse<List<LcmGetAssets200ResponseInnerOneOf1>>(
            HttpStatusCode.BadRequest,
            Headers(("Request-Id", "request-id")),
            null!,
            "invalid request");
        SetupGetConfigs(response);

        var result = await m_Transport.GetConfigsAsync(
            "catalog/", 25, "cursor", false, "schema", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Content, Is.Null);
            Assert.That(result.Error, Is.EqualTo("invalid request"));
            Assert.That(result.Headers["Request-Id"], Is.EqualTo("request-id"));
        });
    }

    [Test]
    public async Task GetConfigsContentAsync_SuccessMapsInlineBodies()
    {
        var response = new ApiResponse<List<LcmGetConfigsContent200ResponseInner>>(
            HttpStatusCode.OK,
            new Multimap<string, string>(),
            new List<LcmGetConfigsContent200ResponseInner>
            {
                ConfigWithContent()
            });
        m_ConfigsApi.Setup(api => api.GetConfigsContentWithHttpInfoAsync(
                k_EnvironmentId, k_ProjectId, null, null, null, 100, "catalog/", "schema", null,
                "after", true, It.Is<List<string>>(tags => IsTagless(tags)), null, null, null, null,
                0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await m_Transport.GetConfigsContentAsync(
            "catalog/", 100, "after", true, "schema", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Content, Has.Count.EqualTo(1));
            Assert.That(result.Content[0].Body, Is.Not.Null);
            Assert.That(result.Content[0].Body!.TryGetContentAs<JObject>(out var body), Is.True);
            Assert.That(body?.Value<string>("uSKU"), Is.EqualTo("test"));
        });
    }

    [Test]
    public async Task GetConfigsContentAsync_FailureReturnsErrorWithoutMappingBody()
    {
        var response = new ApiResponse<List<LcmGetConfigsContent200ResponseInner>>(
            HttpStatusCode.NotFound,
            Headers(("Retry-After", "2")),
            null!,
            "not found");
        SetupGetConfigsContent(response);

        var result = await m_Transport.GetConfigsContentAsync(
            "catalog/", 100, "after", true, "schema", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(404));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Content, Is.Null);
            Assert.That(result.Error, Is.EqualTo("not found"));
            Assert.That(result.Headers["Retry-After"], Is.EqualTo("2"));
        });
    }

    [Test]
    public async Task GetConfigContentAsync_SuccessUsesTaglessVariantAndMapsBody()
    {
        var response = new ApiResponse<Dictionary<string, object>>(
            HttpStatusCode.OK,
            new Multimap<string, string>(),
            new Dictionary<string, object> { ["uSKU"] = "test" });
        m_ConfigsApi.Setup(api => api.GetConfigContentWithHttpInfoAsync(
                k_EnvironmentId, k_ProjectId, k_Path, null, null,
                It.Is<List<string>>(tags => IsTagless(tags)), 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await m_Transport.GetConfigContentAsync(k_Path, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Content.TryGetContentAs<JObject>(out var body), Is.True);
            Assert.That(body?.Value<string>("uSKU"), Is.EqualTo("test"));
        });
    }

    [Test]
    public async Task GetConfigContentAsync_FailureReturnsErrorWithoutMappingBody()
    {
        var response = new ApiResponse<Dictionary<string, object>>(
            HttpStatusCode.NotFound,
            new Multimap<string, string>(),
            null!,
            "not found");
        SetupGetConfigContent(response);

        var result = await m_Transport.GetConfigContentAsync(k_Path, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(404));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Content, Is.Null);
            Assert.That(result.Error, Is.EqualTo("not found"));
        });
    }

    [Test]
    public async Task CreateConfigAsync_SuccessMapsResponseAndSendsDeserializedBody()
    {
        var response = new ApiResponse<LcmGetAssets200ResponseInnerOneOf1>(
            HttpStatusCode.Created,
            Headers(("Request-Id", "request-id")),
            TaglessConfig());
        m_ConfigsApi.Setup(api => api.CreateConfigWithHttpInfoAsync(
                k_EnvironmentId, k_ProjectId, k_Path,
                It.Is<Dictionary<string, object>>(body => HasTestSku(body)),
                null, null, It.Is<List<string>>(tags => IsTagless(tags)), 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await m_Transport.CreateConfigAsync(
            k_Path, "{\"uSKU\":\"test\"}", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(201));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Content.Path, Is.EqualTo(k_Path));
            Assert.That(result.Content.VariantTags, Is.Empty);
            Assert.That(result.Headers["Request-Id"], Is.EqualTo("request-id"));
        });
        m_ConfigsApi.VerifyAll();
    }

    [Test]
    public async Task CreateConfigAsync_FailureReturnsApiExceptionDetails()
    {
        var exception = new ApiException(
            409,
            "Conflict",
            "{\"detail\":\"already exists\"}",
            Headers(("Request-Id", "request-id")));
        m_ConfigsApi.Setup(api => api.CreateConfigWithHttpInfoAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<List<string>?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var result = await m_Transport.CreateConfigAsync(
            k_Path, "{\"uSKU\":\"test\"}", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(409));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Content, Is.Null);
            Assert.That(result.Error, Is.EqualTo("{\"detail\":\"already exists\"}"));
            Assert.That(result.Headers["Request-Id"], Is.EqualTo("request-id"));
        });
    }

    [Test]
    public async Task UpdateConfigAsync_SuccessMapsResponseAndSendsDeserializedBody()
    {
        var response = new ApiResponse<LcmGetAssets200ResponseInnerOneOf1>(
            HttpStatusCode.OK,
            new Multimap<string, string>(),
            TaglessConfig());
        m_ConfigsApi.Setup(api => api.UpdateConfigWithHttpInfoAsync(
                k_EnvironmentId, k_ProjectId, k_Path,
                It.Is<Dictionary<string, object>>(body => HasTestSku(body)),
                null, null, It.Is<List<string>>(tags => IsTagless(tags)), 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await m_Transport.UpdateConfigAsync(
            k_Path, "{\"uSKU\":\"test\"}", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Content.Path, Is.EqualTo(k_Path));
            Assert.That(result.Content.VariantTags, Is.Empty);
        });
        m_ConfigsApi.VerifyAll();
    }

    [Test]
    public async Task UpdateConfigAsync_FailureResponseDoesNotMapBody()
    {
        var response = new ApiResponse<LcmGetAssets200ResponseInnerOneOf1>(
            HttpStatusCode.BadRequest,
            new Multimap<string, string>(),
            null!,
            "invalid config");
        m_ConfigsApi.Setup(api => api.UpdateConfigWithHttpInfoAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<List<string>?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await m_Transport.UpdateConfigAsync(
            k_Path, "{\"uSKU\":\"test\"}", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Content, Is.Null);
            Assert.That(result.Error, Is.EqualTo("invalid config"));
        });
    }

    [Test]
    public void UpdateConfigAsync_SuccessWithoutExactlyOneTaglessVariantThrows()
    {
        var response = new ApiResponse<LcmGetAssets200ResponseInnerOneOf1>(
            HttpStatusCode.OK,
            new Multimap<string, string>(),
            ConfigWithoutTaglessVariant());
        m_ConfigsApi.Setup(api => api.UpdateConfigWithHttpInfoAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<List<string>?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        Assert.ThrowsAsync<PurchasingException>(() => m_Transport.UpdateConfigAsync(
            k_Path, "{}", CancellationToken.None));
    }

    [Test]
    public async Task DeleteConfigAsync_SuccessPreservesStatusAndHeaders()
    {
        var response = new ApiResponse<LcmDeleteConfig200Response>(
            HttpStatusCode.NoContent,
            Headers(("Retry-After", "5")),
            new LcmDeleteConfig200Response());
        m_ConfigsApi.Setup(api => api.DeleteConfigWithHttpInfoAsync(
                k_EnvironmentId, k_ProjectId, k_Path, null, null,
                It.Is<List<string>>(tags => IsTagless(tags)), 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await m_Transport.DeleteConfigAsync(k_Path, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(204));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Headers["Retry-After"], Is.EqualTo("5"));
        });
    }

    [Test]
    public async Task DeleteConfigAsync_FailureReturnsApiExceptionDetails()
    {
        var exception = new ApiException(
            503,
            "Unavailable",
            "{\"detail\":\"try later\"}",
            Headers(("Retry-After", "7")));
        m_ConfigsApi.Setup(api => api.DeleteConfigWithHttpInfoAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<List<string>?>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var result = await m_Transport.DeleteConfigAsync(k_Path, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(503));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo("{\"detail\":\"try later\"}"));
            Assert.That(result.Headers["Retry-After"], Is.EqualTo("7"));
        });
    }

    [Test]
    public void Cancellation_Propagates()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        m_Auth.Setup(auth => auth.GetAccessTokenAsync(cancellation.Token))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));

        Assert.ThrowsAsync<OperationCanceledException>(() =>
            m_Transport.GetConfigContentAsync(k_Path, cancellation.Token));
    }

    void SetupGetConfigs(ApiResponse<List<LcmGetAssets200ResponseInnerOneOf1>> response)
    {
        m_ConfigsApi.Setup(api => api.GetConfigsWithHttpInfoAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<string?>(),
                It.IsAny<bool?>(), It.IsAny<long?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<List<string>?>(), It.IsAny<bool?>(), It.IsAny<string?>(),
                It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    void SetupGetConfigsContent(ApiResponse<List<LcmGetConfigsContent200ResponseInner>> response)
    {
        m_ConfigsApi.Setup(api => api.GetConfigsContentWithHttpInfoAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<long?>(), It.IsAny<long?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<List<string>?>(),
                It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    void SetupGetConfigContent(ApiResponse<Dictionary<string, object>> response)
    {
        m_ConfigsApi.Setup(api => api.GetConfigContentWithHttpInfoAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<List<string>?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    static LcmGetAssets200ResponseInnerOneOf1 TaglessConfig()
    {
        return new LcmGetAssets200ResponseInnerOneOf1(
            id: "config-id",
            path: k_Path,
            contentType: "application/json",
            schemas: new List<string> { "schema" },
            variants: new List<LcmGetAssets200ResponseInnerOneOf1VariantsInner>
            {
                new(
                    complete: true,
                    variantTag: new List<string>(),
                    contentHash: "hash-a",
                    metadata: new Dictionary<string, object> { ["owner"] = "iap" })
            });
    }

    static LcmGetAssets200ResponseInnerOneOf1 GroupedConfig()
    {
        return new LcmGetAssets200ResponseInnerOneOf1(
            id: "config-id",
            path: k_Path,
            contentType: "application/json",
            sortIndex: 7,
            schemas: new List<string> { "schema" },
            variants: new List<LcmGetAssets200ResponseInnerOneOf1VariantsInner>
            {
                new(
                    complete: true,
                    variantTag: new List<string>(),
                    contentHash: "hash-a",
                    metadata: new Dictionary<string, object> { ["owner"] = "iap" }),
                new(
                    complete: true,
                    variantTag: new List<string> { "beta" },
                    contentHash: "hash-b")
            });
    }

    static LcmGetAssets200ResponseInnerOneOf1 ConfigWithoutTaglessVariant()
    {
        return new LcmGetAssets200ResponseInnerOneOf1(
            id: "config-id",
            path: k_Path,
            contentType: "application/json",
            variants: new List<LcmGetAssets200ResponseInnerOneOf1VariantsInner>
            {
                new(
                    complete: true,
                    variantTag: new List<string> { "beta" },
                    contentHash: "hash")
            });
    }

    static LcmGetConfigsContent200ResponseInner ConfigWithContent()
    {
        return new LcmGetConfigsContent200ResponseInner(
            contentType: "application/json",
            id: "config-id",
            path: k_Path,
            schemas: new List<string> { "schema" },
            type: LcmGetConfigsContent200ResponseInner.TypeEnum.Config,
            variants: new List<LcmGetConfigsContent200ResponseInnerVariantsInner>
            {
                new(
                    complete: true,
                    content: new Dictionary<string, object> { ["uSKU"] = "test" },
                    contentHash: "hash",
                    variantTag: new List<string>())
            });
    }

    static Multimap<string, string> Headers(params (string Name, string Value)[] headers)
    {
        var result = new Multimap<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in headers)
        {
            result.Add(name, value);
        }

        return result;
    }

    static bool IsTagless(List<string>? tags)
    {
        return tags is { Count: 0 };
    }

    static bool HasTestSku(Dictionary<string, object>? body)
    {
        return body is not null
            && body.TryGetValue("uSKU", out var value)
            && value?.ToString() == "test";
    }
}
