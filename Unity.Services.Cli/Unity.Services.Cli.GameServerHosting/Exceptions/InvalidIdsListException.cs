using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.GameServerHosting.Exceptions;

public class InvalidDateRangeException : CliException
{
    public InvalidDateRangeException(string input)
        : base($"Invalid date range: '{input}'", Common.Exceptions.ExitCode.HandledError) { }
}
