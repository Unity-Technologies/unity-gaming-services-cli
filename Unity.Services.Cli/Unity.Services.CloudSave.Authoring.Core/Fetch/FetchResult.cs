using System.Collections.Generic;
using Unity.Services.CloudSave.Authoring.Core.Model;

namespace Unity.Services.CloudSave.Authoring.Core.Fetch
{
    public class FetchResult
    {
        public IReadOnlyList<IResourceDeploymentItem> Fetched { get; set; }
    }
}
