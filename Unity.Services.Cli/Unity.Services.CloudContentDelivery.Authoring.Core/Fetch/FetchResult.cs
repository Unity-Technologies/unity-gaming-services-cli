using System.Collections.Generic;
using Unity.Services.CloudContentDelivery.Authoring.Core.Model;

namespace Unity.Services.CloudContentDelivery.Authoring.Core.Fetch
{
    public class FetchResult
    {
        public List<IBucket> Created { get; set; }
        public List<IBucket> Updated { get; set; }
        public List<IBucket> Deleted { get; set; }
        public List<IBucket> Fetched { get; set; }
        public List<IBucket> Failed { get; set; }
    }
}
