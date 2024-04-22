using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.GameServerHosting.Exceptions;

public class InvalidKeyValuePairException : CliException
{
    public InvalidKeyValuePairException(string input)
        : base($"Could not parse key:value pair from input: '{input}'", Common.Exceptions.ExitCode.HandledError) { }
}
