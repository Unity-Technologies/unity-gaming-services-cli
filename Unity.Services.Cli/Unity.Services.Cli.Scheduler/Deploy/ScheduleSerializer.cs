using Newtonsoft.Json;
using Unity.Services.Scheduler.Authoring.Core.Model;
using Unity.Services.Scheduler.Authoring.Core.Serialization;

namespace Unity.Services.Cli.Scheduler.Deploy;

public class SchedulesSerializer : ISchedulesSerializer
{
    public string Serialize(IList<IScheduleConfig> config)
    {
        var file = new ScheduleConfigFile()
        {
            Configs = config.Cast<ScheduleConfig>().ToDictionary(k => k.Name, v => v)
        };
        return JsonConvert.SerializeObject(file, Formatting.Indented);
    }
}
