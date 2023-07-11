using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.GameServerHosting.Exceptions;

[Serializable]
public class InvalidResponseException : CliException
{
    protected InvalidResponseException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    public InvalidResponseException(string reason)
        : base($"Response is invalid: '{reason}'", Common.Exceptions.ExitCode.HandledError) { }
}
