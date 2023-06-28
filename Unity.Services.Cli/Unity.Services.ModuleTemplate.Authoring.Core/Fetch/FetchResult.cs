using System.Collections.Generic;
using Unity.Services.ModuleTemplate.Authoring.Core.Model;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Fetch
{
    public class FetchResult
    {
        public List<IResource> Created { get; set; }
        public List<IResource> Updated { get; set; }
        public List<IResource> Deleted { get; set; }
        public List<IResource> Fetched { get; set; }
        public List<IResource> Failed { get; set; }
    }
}
