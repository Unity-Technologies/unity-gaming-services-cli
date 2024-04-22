using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.GameServerHosting.Exceptions;

public class MissingInputException : CliException
{
    public MissingInputException(string input)
        : base($"Missing value for input: '{input}'", Common.Exceptions.ExitCode.HandledError) { }
}
