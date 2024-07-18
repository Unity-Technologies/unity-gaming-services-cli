using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.GameServerHosting.Exceptions;


public class InvalidLegacyInputUsageException : CliException
{
    public InvalidLegacyInputUsageException(string input)
        : base($"Build Configuration usage settings are invalid. Missing value for input: '{input}'", Common.Exceptions.ExitCode.HandledError) { }
}
