using Newtonsoft.Json;
using Unity.Services.Gateway.TriggersApiV1.Generated.Model;

namespace Unity.Services.Cli.Triggers.Model;

class GetTriggerOutput
{
    readonly TriggerConfig m_Config;

    public GetTriggerOutput(TriggerConfig config)
    {
        m_Config = config;
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(m_Config, Formatting.Indented);
    }
}
