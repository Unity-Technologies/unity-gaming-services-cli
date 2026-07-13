using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEditor.Purchasing.Editor.Authoring.Core;

namespace Unity.Services.Cli.Purchasing.Model;

[DataContract]
class CatalogItemDto
{
    [DataMember(Name = "$schema"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public IList<string>? Schemas { get; set; }

    [DataMember(Name = "uSKU")]
    public string? uSku { get; set; }

    [DataMember(Name = "type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public DtoProductType ProductType { get; set; }

    [DataMember(Name = "productDetails")]
    public List<ProductDetailsDto>? ProductDetails { get; set; }

    [DataMember(Name = "pricing")]
    public List<PricingDetailsDto>? PricingDetails { get; set; }

    [DataMember(Name = "imageUrl"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? ImageUrl { get; set; }

    [DataMember(Name = "storeIdOverrides"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<StoreIdOverrideDto>? StoreIdOverrides { get; set; }
}

[Serializable, DataContract]
class ProductDetailsDto
{
    [DataMember(Name = "title")]
    public string? Title;

    [DataMember(Name = "description"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Description;

    [DataMember(Name = "language")]
    [JsonConverter(typeof(StringEnumConverter))]
    public TranslationLocale Language;

    [DataMember(Name = "subtitle"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Subtitle;

    [DataMember(Name = "badge"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public ProductBadgeDto? Badge;
}

[Serializable, DataContract]
class ProductBadgeDto
{
    [DataMember(Name = "text")]
    public string? Text;

    [DataMember(Name = "imageUrl"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? ImageUrl;
}

[Serializable, DataContract]
class PricingDetailsDto
{
    [DataMember(Name = "currencyCode")]
    public string? CurrencyCode;

    [DataMember(Name = "amount")]
    public long Amount;

    [DataMember(Name = "webshopPrice"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public long? WebshopPrice;
}

[Serializable, DataContract]
class StoreIdOverrideDto
{
    [DataMember(Name = "store")]
    [JsonConverter(typeof(StringEnumConverter))]
    public DtoStoreId Store;

    [DataMember(Name = "value")]
    public string? Value;
}

enum DtoProductType
{
    Consumable,
    NonConsumable,
    Subscription,
    [EnumMember(Value = "non-consumable")]
    NonConsumable2,
    Unknown
}

enum DtoStoreId
{
    [EnumMember(Value = "apple")]
    Apple,
    [EnumMember(Value = "google")]
    Google,
}
