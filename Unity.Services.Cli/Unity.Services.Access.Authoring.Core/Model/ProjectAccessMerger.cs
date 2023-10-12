using System.Collections.Generic;
using System.Linq;

namespace Unity.Services.Access.Authoring.Core.Model
{
    public class ProjectAccessMerger: IProjectAccessMerger
    {
        public List<AccessControlStatement> MergeStatementsToDeploy(
            IReadOnlyList<AccessControlStatement> toCreate,
            IReadOnlyList<AccessControlStatement> toUpdate,
            IReadOnlyList<AccessControlStatement> toDelete,
            IReadOnlyList<AccessControlStatement> remoteStatements)
        {
            var localStatements = toCreate.Concat(toUpdate).ToList();
            var remoteStatementsExceptToDelete = remoteStatements.Except(toDelete).ToList();

            return MergeStatements(localStatements, remoteStatementsExceptToDelete);
        }

        static List<AccessControlStatement> MergeStatements(
            IReadOnlyList<AccessControlStatement> localStatements,
            IReadOnlyList<AccessControlStatement> remoteStatements)
        {
            var localStatementsList = localStatements.ToList();

            var localStatementSids = localStatementsList.Select(statement => statement.Sid).ToList();
            var remoteStatementSids = remoteStatements.Select(statement => statement.Sid).ToList();

            var conflicts = localStatementSids.Intersect(remoteStatementSids);
            var cleanedUpStatementsFromRemote =
                remoteStatements.Where(statement => !conflicts.Contains(statement.Sid));

            var finalStatementList = new List<AccessControlStatement>();

            finalStatementList.AddRange(localStatementsList);
            finalStatementList.AddRange(cleanedUpStatementsFromRemote);

            return finalStatementList;
        }
    }
}
