using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;

namespace Unity.Services.Cli.Purchasing.Authoring;

class CliWebshopCategoriesClient : IWebshopCategoriesClient
{
    internal const string Path = "webshop/categories.json";
    const string k_RequiredSchemaPath = "/v1/schemas/UnityWebshopCategories/versions/1.1.0";

    readonly ILiveContentApiTransport m_Transport;
    readonly string m_RequiredSchema;

    public CliWebshopCategoriesClient(
        ILiveContentApiTransport transport,
        string schemaRegistryBasePath)
    {
        m_Transport = transport;
        m_RequiredSchema = schemaRegistryBasePath + k_RequiredSchemaPath;
    }

    public Task Initialize(string environmentId, string projectId, CancellationToken cancellationToken)
    {
        return m_Transport.InitializeAsync(environmentId, projectId, cancellationToken);
    }

    public async Task<WebshopCategories?> Get(CancellationToken cancellationToken)
    {
        try
        {
            var response = await m_Transport.GetConfigContentAsync(Path, cancellationToken);

            if (response.StatusCode == 404)
            {
                return null;
            }

            if (!response.IsSuccess)
            {
                throw CreateException(response);
            }

            if (response.Content == null)
            {
                return new WebshopCategories();
            }

            return response.Content.TryGetContentAs<WebshopCategories>(out var categories)
                ? categories
                : new WebshopCategories();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ClientException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw CreateException(e);
        }
    }

    public async Task Upsert(WebshopCategories categories, CancellationToken cancellationToken)
    {
        if (categories is null)
        {
            throw new ArgumentNullException(nameof(categories));
        }

        var existingMetadata = await FetchExistingMetadata(cancellationToken);
        var body = SerializeBody(categories, existingMetadata);
        try
        {
            var response = await m_Transport.UpdateConfigAsync(Path, body, cancellationToken);

            if (response.StatusCode == 404)
            {
                response = await m_Transport.CreateConfigAsync(Path, body, cancellationToken);
            }

            if (!response.IsSuccess)
            {
                throw CreateException(response);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ClientException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw CreateException(e);
        }
    }

    async Task<ConfigMetadata> FetchExistingMetadata(CancellationToken cancellationToken)
    {
        try
        {
            var response = await m_Transport.GetConfigContentAsync(Path, cancellationToken);
            if (!response.IsSuccess || response.Content == null)
            {
                return new ConfigMetadata();
            }

            if (response.Content.TryGetContentAs<JObject>(out var raw) && raw["$metadata"] is JObject jMetadata)
            {
                return jMetadata.ToObject<ConfigMetadata>() ?? new ConfigMetadata();
            }

            return new ConfigMetadata();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Best-effort: metadata preservation is non-critical; proceed with fresh metadata on any GET failure.
            return new ConfigMetadata();
        }
    }

    string SerializeBody(WebshopCategories categories, ConfigMetadata existingMetadata)
    {
        var applied = LiveContentMetadata.ApplyManagedBy(JObject.FromObject(existingMetadata));
        var metadata = applied.ToObject<ConfigMetadata>() ?? existingMetadata;

        var envelope = new BodyEnvelope
        {
            Schemas = new List<string> { m_RequiredSchema },
            Metadata = metadata,
            Categories = categories.Categories
        };
        return JsonConvert.SerializeObject(envelope,
            new JsonSerializerSettings { Formatting = Formatting.Indented });
    }

    static ClientException CreateException(Exception e, [CallerMemberName] string? caller = null)
    {
        return new ClientException($"Request '{caller}' failed unexpectedly. {e.Message}", e);
    }

    static ClientException CreateException(ITransportResult response, [CallerMemberName] string? caller = null)
    {
        return new ClientException($"Request '{caller}' failed with '{response.StatusCode}'. {response.Error}", null!);
    }

    internal class ConfigMetadata
    {
        [JsonProperty("managedBy")]
        public string? ManagedBy { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken>? AdditionalProperties { get; set; }
    }

    class BodyEnvelope
    {
        [JsonProperty("$schema", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Schemas { get; set; }

        [JsonProperty("$metadata")]
        public ConfigMetadata? Metadata { get; set; }

        [JsonProperty("categories")]
        public List<WebshopCategory>? Categories { get; set; }
    }
}
