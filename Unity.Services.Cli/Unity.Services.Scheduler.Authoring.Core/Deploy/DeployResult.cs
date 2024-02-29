using System.Collections.Generic;
using Unity.Services.Scheduler.Authoring.Core.Model;

namespace Unity.Services.Scheduler.Authoring.Core.Deploy
{
    public class DeployResult
    {
        public List<IScheduleConfig> Created { get; set; }
        public List<IScheduleConfig> Updated { get; set; }
        public List<IScheduleConfig> Deleted { get; set; }
        public List<IScheduleConfig> Deployed { get; set; }
        public List<IScheduleConfig> Failed { get; set; }
    }
}
