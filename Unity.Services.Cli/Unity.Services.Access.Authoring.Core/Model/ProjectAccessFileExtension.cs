using System.Collections.Generic;
using System.Linq;

namespace Unity.Services.Access.Authoring.Core.Model
{
    public static class ProjectAccessFileExtension
    {
        public static ProjectAccessFileContent ToFileContent(this IProjectAccessFile file)
        {
            return new ProjectAccessFileContent(file.Statements);
        }

        public static void RemoveStatements(this IProjectAccessFile file, IReadOnlyList<AccessControlStatement> statementsToRemove)
        {
            foreach (var statementToRemove in statementsToRemove)
            {
                var index = file.Statements.FindIndex(statement => statement.Sid == statementToRemove.Sid);

                if (index >= 0)
                {
                    statementToRemove.Path = file.Path;
                    statementToRemove.Name = statementToRemove.Sid;
                }
            }

            file.Statements.RemoveAll(statement => statementsToRemove.Any(statementToRemove => statement.Sid == statementToRemove.Sid));
        }

        public static void UpdateStatements(this IProjectAccessFile file, IReadOnlyList<AccessControlStatement> statementsToUpdate)
        {
            foreach (var statementToUpdate in statementsToUpdate)
            {
                var index = file.Statements.FindIndex(statement => statement.Sid == statementToUpdate.Sid);

                if (index >= 0)
                {
                    file.Statements[index] = statementToUpdate;
                    statementToUpdate.Path = file.Path;
                    statementToUpdate.Name = statementToUpdate.Sid;
                }
            }
        }

        public static void UpdateOrCreateStatements(this IProjectAccessFile file, IReadOnlyList<AccessControlStatement> statementsToCreateOrUpdate)
        {
            foreach (var statementToCreateOrUpdate in statementsToCreateOrUpdate)
            {
                var index = file.Statements.FindIndex(statement => statement.Sid == statementToCreateOrUpdate.Sid);
                statementToCreateOrUpdate.Path = file.Path;
                statementToCreateOrUpdate.Name = statementToCreateOrUpdate.Sid;

                if (index >= 0)
                {
                    file.Statements[index] = statementToCreateOrUpdate;
                }
                else
                {
                    file.Statements.Add(statementToCreateOrUpdate);
                }
            }
        }
    }
}
