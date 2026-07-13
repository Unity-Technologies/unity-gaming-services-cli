using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Utils;
using Unity.Services.Cli.Authoring.Exceptions;
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

        try
        {
            JsonConvert.PopulateObject(json, ddef, k_JsonSerializerSettings);

            var variantTags = VariantTagsUtils.FromAdditionalProperties(ddef.AdditionalProperties);
            VariantTagsUtils.Validate(variantTags);
        }
        catch (Exception ex)
        {
            throw new DeployException($"Unexpected error loading deployment definition file '{path}': {ex.Message}");
        }

        return ddef;
    }
}
