using Newtonsoft.Json;
using Unity.Services.Gateway.TriggersApiV1.Generated.Model;

namespace Unity.Services.Cli.Triggers.Model;

class ListDlqEventsOutput
{
    public List<DLQEvent> Events { get; }

    public ListDlqEventsOutput(IEnumerable<DLQEvent> events)
    {
        Events = events.ToList();
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(Events, Formatting.Indented);
    }
}
