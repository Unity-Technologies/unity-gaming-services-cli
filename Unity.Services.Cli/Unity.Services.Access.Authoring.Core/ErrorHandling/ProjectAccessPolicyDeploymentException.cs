using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Unity.Services.Access.Authoring.Core.Model;

namespace Unity.Services.Access.Authoring.Core.ErrorHandling
{
    public abstract class ProjectAccessPolicyDeploymentException : Exception
    {
        public List<IProjectAccessFile> AffectedFiles { get; protected set; }
        public abstract string StatusDescription { get; }
        public abstract string StatusDetail { get; }
        public abstract StatusLevel Level { get; }

        public enum StatusLevel
        {
            Error,
            Warning
        }
        protected ProjectAccessPolicyDeploymentException()
        { }

        protected ProjectAccessPolicyDeploymentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
