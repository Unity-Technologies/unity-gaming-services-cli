using System.Collections.Generic;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Deploy
{
    public class DeployResult
    {
        public IReadOnlyList<IDeploymentItem> Deployed { get; set; }
    }
}
