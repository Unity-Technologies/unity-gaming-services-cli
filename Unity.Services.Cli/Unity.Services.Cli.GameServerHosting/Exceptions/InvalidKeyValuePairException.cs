using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.GameServerHosting.Exceptions;

[Serializable]
public class InvalidKeyValuePairException : CliException
{
    protected InvalidKeyValuePairException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    public InvalidKeyValuePairException(string input)
        : base($"Could not parse key:value pair from input: '{input}'", Common.Exceptions.ExitCode.HandledError) { }
}
