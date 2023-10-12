using System.Collections.Generic;
using Unity.Services.Access.Authoring.Core.ErrorHandling;
using Unity.Services.Access.Authoring.Core.Model;

namespace Unity.Services.Access.Authoring.Core.Validations
{
    public interface IProjectAccessConfigValidator
    {
        List<AccessControlStatement> FilterNonDuplicatedAuthoringStatements(
            IReadOnlyList<IProjectAccessFile> files,
            ICollection<ProjectAccessPolicyDeploymentException> deploymentExceptions);

        bool Validate(
            IProjectAccessFile file,
            ICollection<ProjectAccessPolicyDeploymentException> deploymentExceptions);
    }
}
