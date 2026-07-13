using Newtonsoft.Json;
using Unity.Services.Gateway.TriggersApiV1.Generated.Model;

namespace Unity.Services.Cli.Triggers.Model;

class GetDlqEventOutput
{
    readonly DLQEvent m_Event;

    public GetDlqEventOutput(DLQEvent dlqEvent)
    {
        m_Event = dlqEvent;
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(m_Event, Formatting.Indented);
    }
}
