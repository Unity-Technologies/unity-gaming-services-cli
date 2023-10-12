using System.Collections.Generic;
using Unity.Services.Triggers.Authoring.Core.Model;

namespace Unity.Services.Triggers.Authoring.Core.Fetch
{
    public class FetchResult
    {
        public List<ITriggerConfig> Created { get; set; }
        public List<ITriggerConfig> Updated { get; set; }
        public List<ITriggerConfig> Deleted { get; set; }
        public List<ITriggerConfig> Fetched { get; set; }
        public List<ITriggerConfig> Failed { get; set; }
    }
}
