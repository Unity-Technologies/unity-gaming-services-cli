using System;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Model
{
    public class ClientException : Exception
    {
        public ClientException(string message, Exception innerExcception) : base(message, innerExcception)
        {

        }
    }
}
