using System.Collections.Generic;

namespace Unity.Services.Access.Authoring.Core.Model
{
    public interface IProjectAccessMerger
    {
        List<AccessControlStatement> MergeStatementsToDeploy(
            IReadOnlyList<AccessControlStatement> toCreate,
            IReadOnlyList<AccessControlStatement> toUpdate,
            IReadOnlyList<AccessControlStatement> toDelete,
            IReadOnlyList<AccessControlStatement> remoteStatements);
    }
}
