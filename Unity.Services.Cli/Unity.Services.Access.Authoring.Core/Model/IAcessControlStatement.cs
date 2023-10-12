using System;
using System.Collections.Generic;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Access.Authoring.Core.Model
{
    public interface IAcessControlStatement : IDeploymentItem, ITypedItem
    {
        string Sid { get; set; }
        List<string> Action { get; set; }
        string Effect { get; set; }
        string Principal { get; set; }
        string Resource { get; set; }
        DateTime ExpiresAt { get; set; }
        string Version { get; set; }
    }
}
