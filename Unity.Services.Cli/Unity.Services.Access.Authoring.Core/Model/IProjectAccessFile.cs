using System.Collections.Generic;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Access.Authoring.Core.Model
{
    public interface IProjectAccessFile : IDeploymentItem
    {
        List<AccessControlStatement> Statements { get; set; }

        new float Progress { get; set; }
    }
}
