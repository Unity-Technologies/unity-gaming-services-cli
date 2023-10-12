using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Gateway.EconomyApiV2.Generated.Model;

namespace Unity.Services.Cli.Economy.Model;

class EconomyResourcesResponseResult
{
    public List<GetResourcesResponseResultsInner> Resources;

    public EconomyResourcesResponseResult(List<GetResourcesResponseResultsInner> resources)
    {
        Resources = resources;
    }

    public override string ToString()
    {
        var jsonString = JsonConvert.SerializeObject(Resources);
        var formattedJson = JToken.Parse(jsonString).ToString(Formatting.Indented);
        return formattedJson;
    }
}
