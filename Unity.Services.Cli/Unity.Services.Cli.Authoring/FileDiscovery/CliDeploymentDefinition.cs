using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Deployment.Core.Model;
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

}
