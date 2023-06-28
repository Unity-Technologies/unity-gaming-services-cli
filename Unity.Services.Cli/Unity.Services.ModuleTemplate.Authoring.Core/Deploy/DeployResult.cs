using System.Collections.Generic;
using Unity.Services.ModuleTemplate.Authoring.Core.Model;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Deploy
{
    public class DeployResult
    {
        public List<IResource> Created { get; set; }
        public List<IResource> Updated { get; set; }
        public List<IResource> Deleted { get; set; }
        public List<IResource> Deployed { get; set; }
        public List<IResource> Failed { get; set; }
    }
}
