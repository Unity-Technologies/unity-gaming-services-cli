using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Gateway.TriggersApiV1.Generated.Model;

namespace Unity.Services.Cli.Triggers.Model;

class ListTriggersOutput
{
    public List<TriggerConfigListItem> triggers;

    public ListTriggersOutput(IEnumerable<TriggerConfigListItem> configs)
    {
        triggers = configs.ToList();
    }

    public override string ToString()
    {
        var jsonString = JsonConvert.SerializeObject(triggers);
        return JToken.Parse(jsonString).ToString(Formatting.Indented);
    }
}

