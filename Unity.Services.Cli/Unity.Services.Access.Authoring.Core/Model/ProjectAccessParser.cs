using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Services.Access.Authoring.Core.Model
{
    public class ProjectAccessParser : IProjectAccessParser
    {
        public List<AccessControlStatement> ParseFile(ProjectAccessFileContent content, IProjectAccessFile file)
        {
            var authoringStatements = new List<AccessControlStatement>();
            foreach (var statement in content.Statements)
            {
                statement.Path = file.Path;
                statement.Name = statement.Sid;
                authoringStatements.Add(statement);
            }

            return authoringStatements;
        }
    }
}
