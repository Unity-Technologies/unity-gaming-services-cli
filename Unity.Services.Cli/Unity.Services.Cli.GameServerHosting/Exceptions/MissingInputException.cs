using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.GameServerHosting.Exceptions;

[Serializable]
public class MissingInputException : CliException
{
    protected MissingInputException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    public MissingInputException(string input)
        : base($"Missing value for input: '{input}'", Common.Exceptions.ExitCode.HandledError) { }
}
