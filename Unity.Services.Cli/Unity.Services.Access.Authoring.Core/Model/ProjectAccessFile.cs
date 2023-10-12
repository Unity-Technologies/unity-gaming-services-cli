using System;
using System.Collections.Generic;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Access.Authoring.Core.Model
{
    [Serializable]
    public class ProjectAccessFile : DeploymentItem, IProjectAccessFile
    {
        public ProjectAccessFile()
        {
            Type = "Project Access File";
        }

        public sealed override string Path
        {
            get => base.Path;
            set
            {
                base.Path = value;
                Name = System.IO.Path.GetFileName(value);
            }
        }

        public List<AccessControlStatement> Statements { get; set; }

        public override string ToString()
        {
            return $"'{Path}'";
        }
    }
}
