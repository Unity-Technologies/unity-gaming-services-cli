using System;
using System.Collections.Generic;
using Unity.Services.Access.Authoring.Core.Model;

namespace Unity.Services.Access.Authoring.Core.Results
{
    public class Result
    {
        public IReadOnlyList<AccessControlStatement> Created { get; }
        public IReadOnlyList<AccessControlStatement> Updated { get; }
        public IReadOnlyList<AccessControlStatement> Deleted { get; }

        public IReadOnlyList<IProjectAccessFile> Failed { get; }

        public Result(
            IReadOnlyList<AccessControlStatement> created,
            IReadOnlyList<AccessControlStatement> updated,
            IReadOnlyList<AccessControlStatement> deleted,
            IReadOnlyList<IProjectAccessFile> failed = null)
        {
            Created = created;
            Updated = updated;
            Deleted = deleted;
            Failed = failed ?? Array.Empty<IProjectAccessFile>();
        }
    }
}
