using System.ComponentModel;
using Newtonsoft.Json;
using Unity.Services.Cli.Authoring.Templates;
using Unity.Services.Economy.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.Economy.Templates;

class EconomyCurrencyFile : EconomyResourceFile, IFileTemplate
{
    [JsonProperty("$schema")]
    public string Value = "https://ugs-config-schemas.unity3d.com/v1/economy/economy-currency.schema.json";

    [DefaultValue(0)]
    public long Initial { get; set; }
    public long? Max { get; set; }

    [JsonIgnore]
    public string Extension => EconomyResourcesExtensions.Currency;

    [JsonIgnore]
    public string FileBodyText
    {
        get
        {
            var currency = new EconomyCurrencyFile
            {
                Name = "My Currency",
                Initial = 1,
                Max = 50
            };
            return JsonConvert.SerializeObject(currency, GetSerializationSettings());
        }
    }

    [JsonConstructor]
    public EconomyCurrencyFile() : base(EconomyResourceTypes.Currency) { }
}
