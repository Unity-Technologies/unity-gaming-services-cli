using Newtonsoft.Json;
using Unity.Services.Cli.Authoring.Templates;
using Unity.Services.Economy.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.Economy.Templates;

class EconomyInventoryItemFile : EconomyResourceFile, IFileTemplate
{
    [JsonProperty("$schema")]
    public string Value = "https://ugs-config-schemas.unity3d.com/v1/economy/economy-inventory.schema.json";

    [JsonIgnore]
    public string Extension => EconomyResourcesExtensions.InventoryItem;

    [JsonIgnore]
    public string FileBodyText
    {
        get
        {
            var inventory = new EconomyInventoryItemFile();
            inventory.Name = "My Item";
            return JsonConvert.SerializeObject(inventory, GetSerializationSettings());
        }
    }

    [JsonConstructor]
    public EconomyInventoryItemFile()
        : base(EconomyResourceTypes.InventoryItem)
    {
    }
}
