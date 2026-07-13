using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Gateway.LiveContentApiV1.Generated.Model;
using WireMock.Admin.Mappings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;

namespace Unity.Services.Cli.MockServer.ServiceMocks;

public class PurchasingApiMock : IServiceApiMock
{
    const string k_BasePath = "/v1";
    const string k_TestItemPath = "catalog/test-item";

    const string k_CatalogItemDtoJson = """
        {
            "$schema": ["https://services.api.unity.com/schema-registry/v1/schemas/UnityRemoteCatalog/versions/1.1.0"],
            "uSKU": "test-item",
            "type": "Consumable",
            "productDetails": [{"title": "Title", "description": "Description", "language": "en-US"}],
            "pricing": [{"currencyCode": "USD", "amount": 4990000}],
            "$metadata": {"managedBy": "In App Purchase"}
        }
        """;

    static readonly LcmGetAssets200ResponseInner k_CatalogConfigSample = new()
    {
        Id = "aaaa-bbbb-cccc-dddd",
        Path = k_TestItemPath,
        Type = LcmGetAssets200ResponseInner.TypeEnum.Config,
        ContentHash = "abc123",
        ContentSize = 200,
        VariantTag = [],
        CreatedAt = DateTime.MinValue,
        UpdatedAt = DateTime.MinValue,
        Complete = true,
        Metadata = new Dictionary<string, object>
        {
            ["managedBy"] = "In App Purchase"
        }
    };

    public Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        return Task.FromResult<IReadOnlyList<MappingModel>>(Array.Empty<MappingModel>());
    }

    public void CustomMock(WireMockServer mockServer)
    {
        var responseHeaders = new Dictionary<string, WireMockList<string>>
        {
            { "Content-Type", new WireMockList<string>("application/json") },
        };

        MockCatalogList(mockServer, responseHeaders);
        MockCatalogGetContent(mockServer, responseHeaders);
        MockCatalogCreate(mockServer, responseHeaders);
        MockCatalogUpdate(mockServer, responseHeaders);
        MockCatalogDelete(mockServer, responseHeaders);
    }

    static void MockCatalogList(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        var listHeaders = new Dictionary<string, WireMockList<string>>(responseHeaders)
        {
            { "Content-Range", new WireMockList<string>("items 0-0/1") },
        };

        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/info")
                    .WithParam("path", "catalog/")
                    .UsingGet())
            .AtPriority(1)
            .RespondWith(
                Response.Create()
                    .WithHeaders(listHeaders)
                    .WithBodyAsJson(new List<LcmGetAssets200ResponseInner> { k_CatalogConfigSample })
                    .WithStatusCode(200));
    }

    static void MockCatalogGetContent(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/content/{k_TestItemPath}")
                    .UsingGet())
            .AtPriority(1)
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBody(k_CatalogItemDtoJson)
                    .WithStatusCode(200));
    }

    static void MockCatalogCreate(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/info/{k_TestItemPath}")
                    .UsingPost())
            .AtPriority(1)
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_CatalogConfigSample)
                    .WithStatusCode(201));
    }

    static void MockCatalogUpdate(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/info/{k_TestItemPath}")
                    .UsingPut())
            .AtPriority(1)
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithBodyAsJson(k_CatalogConfigSample)
                    .WithStatusCode(200));
    }

    static void MockCatalogDelete(
        WireMockServer mockServer,
        Dictionary<string, WireMockList<string>> responseHeaders)
    {
        mockServer
            .Given(
                Request.Create()
                    .WithPath(
                        $"{k_BasePath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs/info/{k_TestItemPath}")
                    .UsingDelete())
            .AtPriority(1)
            .RespondWith(
                Response.Create()
                    .WithHeaders(responseHeaders)
                    .WithStatusCode(204));
    }
}
