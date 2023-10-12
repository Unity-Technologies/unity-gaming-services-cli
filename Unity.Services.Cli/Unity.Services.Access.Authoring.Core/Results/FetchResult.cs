using System;
using System.Collections.Generic;
using Unity.Services.Access.Authoring.Core.Model;

namespace Unity.Services.Access.Authoring.Core.Results
{
    public class FetchResult : Result
    {
        public IReadOnlyList<IProjectAccessFile> Fetched { get; }

        public FetchResult(
            IReadOnlyList<AccessControlStatement> created,
            IReadOnlyList<AccessControlStatement> updated,
            IReadOnlyList<AccessControlStatement> deleted,
            IReadOnlyList<IProjectAccessFile> fetched = null,
            IReadOnlyList<IProjectAccessFile> failed = null) : base(created, updated,deleted)
        {
            Fetched = fetched ?? Array.Empty<IProjectAccessFile>();
        }
    }
}
