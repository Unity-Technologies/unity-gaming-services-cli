using Newtonsoft.Json;
using Unity.Services.Gateway.TriggersApiV1.Generated.Model;

namespace Unity.Services.Cli.Triggers.Model;

class ReplayAllDlqEventsOutput
{
    readonly DLQQueuedResult m_Result;

    public ReplayAllDlqEventsOutput(DLQQueuedResult result)
    {
        m_Result = result;
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(m_Result, Formatting.Indented);
    }
}
