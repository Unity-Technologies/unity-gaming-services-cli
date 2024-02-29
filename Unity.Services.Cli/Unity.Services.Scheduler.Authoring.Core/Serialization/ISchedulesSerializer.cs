using System.Collections.Generic;
using Unity.Services.Scheduler.Authoring.Core.Model;

namespace Unity.Services.Scheduler.Authoring.Core.Serialization
{
    public interface ISchedulesSerializer
    {
        string Serialize(IList<IScheduleConfig> config);
    }
}
