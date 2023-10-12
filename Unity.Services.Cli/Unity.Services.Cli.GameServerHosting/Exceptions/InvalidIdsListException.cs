using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.GameServerHosting.Exceptions;

[Serializable]
public class InvalidDateRangeException : CliException
{
    protected InvalidDateRangeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    public InvalidDateRangeException(string input)
        : base($"Invalid date range: '{input}'", Common.Exceptions.ExitCode.HandledError) { }
}
