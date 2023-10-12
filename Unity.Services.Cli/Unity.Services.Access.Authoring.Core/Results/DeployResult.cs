using System.Collections.Generic;
using Unity.Services.Access.Authoring.Core.Model;

namespace Unity.Services.Access.Authoring.Core.Results
{
    public class DeployResult
    {
        public IReadOnlyList<AccessControlStatement> Created { get; set; }
        public IReadOnlyList<AccessControlStatement> Updated { get; set; }
        public IReadOnlyList<AccessControlStatement> Deleted { get; set; }
        public IReadOnlyList<IProjectAccessFile> Deployed { get; set; }
        public IReadOnlyList<IProjectAccessFile> Failed { get; set; }
    }
}
