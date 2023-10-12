using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Unity.Services.Access.Authoring.Core.Model;

namespace Unity.Services.Access.Authoring.Core.ErrorHandling
{
    [Serializable]
    public class InvalidDataException : ProjectAccessPolicyDeploymentException
    {
        readonly IProjectAccessFile m_File;
        readonly string m_ErrorMessage;

        public override string Message => $"{StatusDescription} {StatusDetail}";


        public override string StatusDescription => "Invalid Data.";

        public override string StatusDetail => $"The file {m_File.Name} contains Invalid Data: {m_ErrorMessage}";
        public override StatusLevel Level => StatusLevel.Error;

        public InvalidDataException(IProjectAccessFile file, string errorMessage)
        {
            m_File = file;
            m_ErrorMessage = errorMessage;
            AffectedFiles = new List<IProjectAccessFile> {file};
        }

        protected InvalidDataException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
