using System.Collections.Generic;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Fetch
{
    public class FetchResult
    {
        public IReadOnlyList<IDeploymentItem> Fetched { get; set; }
    }
}
