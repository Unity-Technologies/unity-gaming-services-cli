using System.Collections.Generic;
using Unity.Services.Scheduler.Authoring.Core.Model;

namespace Unity.Services.Scheduler.Authoring.Core.Fetch
{
    public class FetchResult
    {
        public List<IScheduleConfig> Created { get; set; }
        public List<IScheduleConfig> Updated { get; set; }
        public List<IScheduleConfig> Deleted { get; set; }
        public List<IScheduleConfig> Fetched { get; set; }
        public List<IScheduleConfig> Failed { get; set; }
    }
}
