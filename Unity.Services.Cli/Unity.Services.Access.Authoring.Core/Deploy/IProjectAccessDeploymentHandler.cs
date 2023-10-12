using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Access.Authoring.Core.Model;
using Unity.Services.Access.Authoring.Core.Results;

namespace Unity.Services.Access.Authoring.Core.Deploy
{
    public interface IProjectAccessDeploymentHandler
    {
        Task<DeployResult> DeployAsync(
            IReadOnlyList<IProjectAccessFile> files,
            bool dryRun = false,
            bool reconcile = false);
    }
}
