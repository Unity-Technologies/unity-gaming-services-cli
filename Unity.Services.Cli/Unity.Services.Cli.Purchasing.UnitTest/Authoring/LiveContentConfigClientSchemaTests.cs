using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Networking;
using UnityEditor.Purchasing.Editor.Authoring.Core;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;
using CoreLogger = UnityEditor.Purchasing.Editor.Authoring.Core.Logger;

namespace Unity.Services.Cli.Purchasing.UnitTest.Authoring;

/// <summary>
/// Verifies that the Live Content client built by the module swaps the local <c>$schema</c>
/// (CDN URL used for editor autocompletion) for the schema-registry URL expected by Live Content,
/// resolved from the current CLI network target.
/// </summary>
[TestFixture]
class LiveContentConfigClientSchemaTests
{
    const string k_CdnSchema = "https://ugs-config-schemas.unity3d.com/v1/purchasing-catalog.schema.json";
    const string k_RequiredSchemaPath = "/v1/schemas/UnityRemoteCatalog/versions/1.1.0";

    CapturingTransport m_Transport = null!;
    ILiveContentConfigClient m_Client = null!;

    [SetUp]
    public async Task SetUp()
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(
        [
            new LiveContentApiEndpoints(),
            new SchemaRegistryApiEndpoints(),
        ]);

        m_Transport = new CapturingTransport();
        m_Client = PurchasingModule.CreateLiveContentConfigClient(
            m_Transport,
            new Mock<CoreLogger.ILogger>().Object);
        await m_Client.Initialize("env-id", "project-id", CancellationToken.None);
    }

    [Test]
    public void CatalogItem_LocalSchemaIsCdnUrl()
    {
        Assert.That(new CatalogItem().Schema, Is.EqualTo(k_CdnSchema));
    }

    [Test]
    public async Task Upsert_Create_ReplacesCdnSchemaWithSchemaRegistryUrl()
    {
        await m_Client.Upsert(CreateItem(), CancellationToken.None);

        Assert.That(m_Transport.CreatedBodies, Has.Count.EqualTo(1));
        AssertSchemaIsSchemaRegistry(m_Transport.CreatedBodies[0]);
    }

    [Test]
    public async Task Upsert_Update_ReplacesCdnSchemaWithSchemaRegistryUrl()
    {
        m_Transport.ExistingContent = new JObject
        {
            ["$schema"] = new JArray(ExpectedRequiredSchema()),
            ["uSKU"] = "test-item",
        };

        await m_Client.Upsert(CreateItem(), CancellationToken.None);

        Assert.That(m_Transport.UpdatedBodies, Has.Count.EqualTo(1));
        AssertSchemaIsSchemaRegistry(m_Transport.UpdatedBodies[0]);
    }

    [Test]
    public async Task List_FiltersBySchemaRegistryUrl()
    {
        await m_Client.List(CancellationToken.None);

        Assert.That(m_Transport.ListSchemaFilters, Has.Count.EqualTo(1));
        Assert.That(m_Transport.ListSchemaFilters[0], Is.EqualTo(ExpectedRequiredSchema()));
    }

    static void AssertSchemaIsSchemaRegistry(string body)
    {
        var json = JObject.Parse(body);
        var schemas = json["$schema"]!.ToObject<List<string>>()!;

        Assert.That(schemas, Is.EqualTo(new[] { ExpectedRequiredSchema() }));
        Assert.That(body, Does.Not.Contain(k_CdnSchema));
    }

    static string ExpectedRequiredSchema()
    {
        return PurchasingModule.GetSchemaRegistryBasePath() + k_RequiredSchemaPath;
    }

    static CatalogItem CreateItem()
    {
        return new CatalogItem
        {
            CatalogListingId = "catalog/test-item",
            uSku = "test-item",
            ProductType = ProductType.Consumable,
            ProductDetails = new List<ProductDetails>
            {
                new() { Title = "Title", Description = "Description", Language = TranslationLocale.en_US },
            },
            PricingDetails = new List<PricingDetails>
            {
                new() { CurrencyCode = "USD", Amount = 4.99 },
            },
        };
    }

    class CapturingTransport : ILiveContentApiTransport
    {
        public JObject? ExistingContent;
        public List<string> CreatedBodies = new();
        public List<string> UpdatedBodies = new();
        public List<string> ListSchemaFilters = new();

        public Task InitializeAsync(string environmentId, string projectId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<TransportResult<IReadOnlyList<LiveContentConfig>>> GetConfigsAsync(
            string pathPrefix, int limit, string after, bool? start, string schema, CancellationToken cancellationToken)
            => Task.FromResult(new TransportResult<IReadOnlyList<LiveContentConfig>>(200, Array.Empty<LiveContentConfig>()));

        public Task<TransportResult<IReadOnlyList<LiveContentConfig>>> GetConfigsContentAsync(
            string pathPrefix, int limit, string after, bool? start, string schema, CancellationToken cancellationToken)
        {
            ListSchemaFilters.Add(schema);
            return Task.FromResult(new TransportResult<IReadOnlyList<LiveContentConfig>>(200, Array.Empty<LiveContentConfig>()));
        }

        public Task<TransportResult<LiveContentConfigBody>> GetConfigContentAsync(string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistingContent == null
                ? new TransportResult<LiveContentConfigBody>(404, error: "not found")
                : new TransportResult<LiveContentConfigBody>(200, new LiveContentConfigBody(ExistingContent)));
        }

        public Task<TransportResult<LiveContentConfig>> CreateConfigAsync(string path, string jsonContent, CancellationToken cancellationToken)
        {
            CreatedBodies.Add(jsonContent);
            return Task.FromResult(new TransportResult<LiveContentConfig>(201, new LiveContentConfig(path: path)));
        }

        public Task<TransportResult<LiveContentConfig>> UpdateConfigAsync(string path, string jsonContent, CancellationToken cancellationToken)
        {
            UpdatedBodies.Add(jsonContent);
            return Task.FromResult(new TransportResult<LiveContentConfig>(200, new LiveContentConfig(path: path)));
        }

        public Task<TransportResult> DeleteConfigAsync(string path, CancellationToken cancellationToken)
            => Task.FromResult(new TransportResult(204));
    }
}
