using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.GameServerHosting.Exceptions;

public class InvalidResponseException : CliException
{
    public InvalidResponseException(string reason)
        : base($"Response is invalid: '{reason}'", Common.Exceptions.ExitCode.HandledError) { }
}
