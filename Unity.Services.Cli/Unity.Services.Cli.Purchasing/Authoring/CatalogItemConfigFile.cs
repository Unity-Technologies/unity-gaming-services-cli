using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Services.Cli.Authoring.Templates;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.Purchasing.Authoring;

[Serializable]
class CatalogItemConfigFile : CatalogItem, IFileTemplate
{
    [JsonIgnore]
    public string Extension => Constants.FileExtension;

    [JsonIgnore]
    public string FileBodyText
    {
        get
        {
            var settings = GetSerializationSettings();
            var defaultJson = JsonConvert.SerializeObject(CreateDefaultCatalog(), settings);
            var configFile = JsonConvert.DeserializeObject<CatalogItemConfigFile>(defaultJson)!;
            return JsonConvert.SerializeObject(configFile, settings);
        }
    }

    public static JsonSerializerSettings GetSerializationSettings()
    {
        var settings = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() },
            Formatting = Formatting.Indented,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
        };
        return settings;
    }
}
