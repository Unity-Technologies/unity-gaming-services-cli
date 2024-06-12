using System;

namespace Unity.Services.CloudSave.Authoring.Core.Model
{
    public class ClientException : Exception
    {
        public ClientException(string message, Exception innerExcception) : base(message, innerExcception)
        {

        }
    }
}
