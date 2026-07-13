using Unity.Services.Cli.Purchasing.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using CoreProductType = UnityEditor.Purchasing.Editor.Authoring.Core.ProductType;
using CoreStoreId = UnityEditor.Purchasing.Editor.Authoring.Core.StoreId;

namespace Unity.Services.Cli.Purchasing.Authoring;

static class CatalogItemDtoConverter
{
    internal const string RequiredSchema =
        "https://services.api.unity.com/schema-registry/v1/schemas/UnityRemoteCatalog/versions/1.1.0";

    public static CatalogItem ConvertFromDto(CatalogItemDto dto)
    {
        return new CatalogItem
        {
            uSku = dto.uSku,
            ProductType = ConvertTypeFromDto(dto.ProductType),
            PricingDetails = dto.PricingDetails?.Select(ConvertPricingFromDto).ToList()
                             ?? new List<PricingDetails>(),
            ProductDetails = dto.ProductDetails?.Select(ConvertProductDetailsFromDto).ToList()
                             ?? new List<ProductDetails>(),
            ImageUrl = dto.ImageUrl,
            StoreIdOverrides = dto.StoreIdOverrides?.Select(ConvertStoreIdFromDto).ToList(),
        };
    }

    public static CatalogItemDto ConvertToDto(CatalogItem item)
    {
        return new CatalogItemDto
        {
            Schemas = new List<string> { RequiredSchema },
            uSku = item.uSku,
            ProductType = ConvertTypeToDto(item.ProductType),
            PricingDetails = item.PricingDetails?.Select(ConvertPricingToDto).ToList()
                             ?? new List<PricingDetailsDto>(),
            ProductDetails = item.ProductDetails?.Select(ConvertProductDetailsToDto).ToList()
                             ?? new List<ProductDetailsDto>(),
            ImageUrl = NullIfEmpty(item.ImageUrl),
            StoreIdOverrides = ConvertStoreIdOverridesToDto(item.StoreIdOverrides),
        };
    }

    static PricingDetails ConvertPricingFromDto(PricingDetailsDto pd)
    {
        return new PricingDetails
        {
            CurrencyCode = pd.CurrencyCode,
            Amount = pd.Amount / 1_000_000D,
            WebshopPrice = pd.WebshopPrice.HasValue ? pd.WebshopPrice.Value / 1_000_000D : 0,
        };
    }

    static ProductDetails ConvertProductDetailsFromDto(ProductDetailsDto pd)
    {
        return new ProductDetails
        {
            Title = pd.Title,
            Description = pd.Description,
            Language = pd.Language,
            Subtitle = pd.Subtitle,
            Badge = pd.Badge == null
                ? null
                : new ProductBadge { Text = pd.Badge.Text, ImageUrl = pd.Badge.ImageUrl },
        };
    }

    static StoreIdOverride ConvertStoreIdFromDto(StoreIdOverrideDto s)
    {
        return new StoreIdOverride
        {
            Store = s.Store switch
            {
                DtoStoreId.Apple => CoreStoreId.Apple,
                DtoStoreId.Google => CoreStoreId.Google,
                _ => throw new ArgumentOutOfRangeException(nameof(s.Store), s.Store, null),
            },
            Value = s.Value,
        };
    }

    static PricingDetailsDto ConvertPricingToDto(PricingDetails pd)
    {
        return new PricingDetailsDto
        {
            CurrencyCode = pd.CurrencyCode,
            Amount = ToMicros(pd.Amount),
            WebshopPrice = pd.IsWebshopPriceSet ? ToMicros(pd.WebshopPrice) : null,
        };
    }

    static long ToMicros(double amount) =>
        checked((long)Math.Round(amount * 1_000_000D, MidpointRounding.AwayFromZero));

    static ProductDetailsDto ConvertProductDetailsToDto(ProductDetails pd)
    {
        return new ProductDetailsDto
        {
            Title = pd.Title,
            Description = NullIfEmpty(pd.Description),
            Language = pd.Language,
            Subtitle = NullIfEmpty(pd.Subtitle),
            Badge = pd.Badge == null || string.IsNullOrEmpty(pd.Badge.Text)
                ? null
                : new ProductBadgeDto { Text = pd.Badge.Text, ImageUrl = NullIfEmpty(pd.Badge.ImageUrl) },
        };
    }

    static List<StoreIdOverrideDto>? ConvertStoreIdOverridesToDto(List<StoreIdOverride>? overrides)
    {
        if (overrides is null)
            return null;
        var result = new List<StoreIdOverrideDto>(overrides.Count);
        foreach (var o in overrides)
        {
            if (o is null || string.IsNullOrEmpty(o.Value))
                continue;
            result.Add(new StoreIdOverrideDto
            {
                Store = o.Store switch
                {
                    CoreStoreId.Apple => DtoStoreId.Apple,
                    CoreStoreId.Google => DtoStoreId.Google,
                    _ => throw new ArgumentOutOfRangeException(nameof(o.Store), o.Store, null),
                },
                Value = o.Value,
            });
        }
        return result.Count == 0 ? null : result;
    }

    static DtoProductType ConvertTypeToDto(CoreProductType productType)
    {
        return productType switch
        {
            CoreProductType.Consumable => DtoProductType.Consumable,
            CoreProductType.NonConsumable => DtoProductType.NonConsumable,
            CoreProductType.Subscription => DtoProductType.Subscription,
            CoreProductType.Unknown => DtoProductType.Unknown,
            _ => throw new ArgumentOutOfRangeException(nameof(productType), productType, null),
        };
    }

    static CoreProductType ConvertTypeFromDto(DtoProductType productType)
    {
        return productType switch
        {
            DtoProductType.Consumable => CoreProductType.Consumable,
            DtoProductType.NonConsumable => CoreProductType.NonConsumable,
            DtoProductType.NonConsumable2 => CoreProductType.NonConsumable,
            DtoProductType.Subscription => CoreProductType.Subscription,
            DtoProductType.Unknown => CoreProductType.Unknown,
            _ => throw new ArgumentOutOfRangeException(nameof(productType), productType, null),
        };
    }

    static string? NullIfEmpty(string? s) => string.IsNullOrEmpty(s) ? null : s;
}
