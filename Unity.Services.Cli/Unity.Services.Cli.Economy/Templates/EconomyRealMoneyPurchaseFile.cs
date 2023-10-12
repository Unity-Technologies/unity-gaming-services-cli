using Newtonsoft.Json;
using Unity.Services.Cli.Authoring.Templates;
using Unity.Services.Economy.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.Economy.Templates;

class EconomyRealMoneyPurchaseFile : EconomyResourceFile, IFileTemplate
{
    [JsonProperty("$schema")]
    public string Value = "https://ugs-config-schemas.unity3d.com/v1/economy/economy-real-purchase.schema.json";

    [JsonRequired]
    public StoreIdentifiers StoreIdentifiers;
    [JsonRequired]
    public RealMoneyReward[] Rewards;

    [JsonIgnore]
    public string Extension => EconomyResourcesExtensions.MoneyPurchase;

    [JsonIgnore]
    public string FileBodyText
    {
        get
        {
            var virtualPurchase = new EconomyRealMoneyPurchaseFile
            {
                Name = "My Real Money Purchase",
                StoreIdentifiers = new()
                {
                    GooglePlayStore = "123"
                },
                Rewards = new[]
                {
                    new RealMoneyReward
                    {
                        ResourceId = "MY_RESOURCE_ID",
                        Amount = 6
                    }
                }
            };

            return JsonConvert.SerializeObject(virtualPurchase, GetSerializationSettings());
        }
    }

    [JsonConstructor]
    public EconomyRealMoneyPurchaseFile()
        : base(EconomyResourceTypes.MoneyPurchase)
    {
        StoreIdentifiers = new StoreIdentifiers();
        Rewards = Array.Empty<RealMoneyReward>();
    }
}
