using System.Collections.Generic;
using Unity.Services.Triggers.Authoring.Core.Model;

namespace Unity.Services.Triggers.Authoring.Core.Deploy
{
    public class DeployResult
    {
        public List<ITriggerConfig> Created { get; set; }
        public List<ITriggerConfig> Updated { get; set; }
        public List<ITriggerConfig> Deleted { get; set; }
        public List<ITriggerConfig> Deployed { get; set; }
        public List<ITriggerConfig> Failed { get; set; }
    }
}
