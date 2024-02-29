using Newtonsoft.Json;
using Unity.Services.Triggers.Authoring.Core.Model;
using Unity.Services.Triggers.Authoring.Core.Serialization;

namespace Unity.Services.Cli.Triggers.Deploy;

public class TriggersSerializer : ITriggersSerializer
{
    public string Serialize(IList<ITriggerConfig> config)
    {
        var file = new TriggersConfigFile()
        {
            Configs = config.Cast<TriggerConfig>().ToList()
        };
        return file.FileBodyText;
    }
}
