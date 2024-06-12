using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.Matchmaker.Service;

[Serializable]
public class MatchmakerException : CliException
{
    protected MatchmakerException(SerializationInfo info, StreamingContext context) : base(Common.Exceptions.ExitCode.HandledError) { }

    public MatchmakerException(int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(exitCode) { }

    /// <summary>
    /// <see cref="MatchmakerException"/> constructor.
    /// </summary>
    /// <param name="message">A message with instructions to guide user how to fix the operation.</param>
    /// <param name="exitCode">Exit code when this exception triggered. Default value is HandledError.</param>
    public MatchmakerException(string message, int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(message, exitCode) { }

    public MatchmakerException(
        string message, Exception innerException, int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(message, innerException, exitCode) { }
}
