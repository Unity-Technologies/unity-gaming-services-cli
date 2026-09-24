using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Unity.Services.Deployment.Core.Model;
using Unity.Services.Deployment.Core.VariantTags;
using Unity.Services.Cli.Authoring.Utils;

namespace Unity.Services.Cli.Authoring.Model;

class CliDeploymentDefinition : IDeploymentDefinition
{
    [JsonExtensionData]
    Dictionary<string, JToken> m_ExtensionData = new();

    public string Name { get; set; }

    [JsonIgnore]
    public string Path { get; set; }

    public ObservableCollection<string> ExcludePaths { get; }

    [JsonIgnore]
    public IReadOnlyDictionary<string, object> AdditionalProperties =>
        new ReadOnlyDictionary<string, object>(
            m_ExtensionData.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToValue()
            )
        );

    public CliDeploymentDefinition(string path)
    {
        Path = path;
        Name = "";
        ExcludePaths = new ObservableCollection<string>();
    }

    internal static CliDeploymentDefinition CreateTemplate(string name)
    {
        var ddef = new CliDeploymentDefinition(string.Empty)
        {
            Name = name
        };

        return ddef;
    }

    internal string Serialize()
        => JsonConvert.SerializeObject(this, k_JsonSerializerSettings);

    static readonly JsonSerializerSettings k_JsonSerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

}
