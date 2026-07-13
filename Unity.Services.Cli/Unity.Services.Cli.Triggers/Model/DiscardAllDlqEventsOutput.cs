using Newtonsoft.Json;
using Unity.Services.Gateway.TriggersApiV1.Generated.Model;

namespace Unity.Services.Cli.Triggers.Model;

class DiscardAllDlqEventsOutput
{
    readonly DLQDiscardResult m_Result;

    public DiscardAllDlqEventsOutput(DLQDiscardResult result)
    {
        m_Result = result;
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(m_Result, Formatting.Indented);
    }
}
