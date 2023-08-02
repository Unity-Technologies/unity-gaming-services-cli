using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Cli.Authoring.DeploymentDefinition;

class DeploymentDefinitionFactory : IDeploymentDefinitionFactory
{
    static readonly JsonSerializerSettings k_JsonSerializerSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };
    public IDeploymentDefinition CreateDeploymentDefinition(string path)
    {
        var ddef = new CliDeploymentDefinition(path);
        var json = File.ReadAllText(path);
        JsonConvert.PopulateObject(json, ddef, k_JsonSerializerSettings);

        return ddef;
    }
}
