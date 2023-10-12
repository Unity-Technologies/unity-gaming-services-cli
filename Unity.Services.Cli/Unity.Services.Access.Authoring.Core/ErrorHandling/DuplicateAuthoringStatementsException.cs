using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Unity.Services.Access.Authoring.Core.Model;

namespace Unity.Services.Access.Authoring.Core.ErrorHandling
{
    [Serializable]
    public class DuplicateAuthoringStatementsException : ProjectAccessPolicyDeploymentException
    {
        readonly string m_Sid;

        public override string Message => $"{StatusDescription} {StatusDetail}";

        public override string StatusDescription => "Duplicate Sid in files.";
        public override StatusLevel Level => StatusLevel.Error;

        public override string StatusDetail
        {
            get
            {
                var builder = new StringBuilder();
                builder.Append($"Multiple resources with the same identifier '{m_Sid}' were found. ");
                builder.Append("Only a single resource for a given identifier may be deployed/fetched at the same time. ");
                builder.Append("Give all resources unique identifiers or deploy/fetch them separately to proceed.\n");

                foreach (var file in AffectedFiles)
                {
                    builder.Append($" '{file.Path}'");
                }

                return builder.ToString();
            }
        }

        public DuplicateAuthoringStatementsException(string sid, IReadOnlyList<IProjectAccessFile> files)
        {
            m_Sid = sid;
            AffectedFiles = new List<IProjectAccessFile>(files);
        }

        protected DuplicateAuthoringStatementsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
