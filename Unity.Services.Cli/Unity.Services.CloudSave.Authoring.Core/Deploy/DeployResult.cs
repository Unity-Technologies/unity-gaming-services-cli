using System.Collections.Generic;
using Unity.Services.CloudSave.Authoring.Core.Model;

namespace Unity.Services.CloudSave.Authoring.Core.Deploy
{
    public class DeployResult
    {
        public IReadOnlyList<IResourceDeploymentItem> Deployed { get; set; }
    }
}
