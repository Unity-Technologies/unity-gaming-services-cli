using System.Collections.Generic;

namespace Unity.Services.Access.Authoring.Core.Model
{
    public interface IProjectAccessParser
    {
        List<AccessControlStatement> ParseFile(ProjectAccessFileContent content, IProjectAccessFile file);
    }
}
