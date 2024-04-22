using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.Triggers.Exceptions;

public class TriggersException : CliException
{
    public TriggersException(int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(exitCode) { }

    /// <summary>
    /// <see cref="TriggersException"/> constructor.
    /// </summary>
    /// <param name="message">A message with instructions to guide user how to fix the operation.</param>
    /// <param name="exitCode">Exit code when this exception triggered. Default value is HandledError.</param>
    public TriggersException(string message, int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(message, exitCode) { }

    public TriggersException(
        string message, Exception innerException, int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(message, innerException, exitCode) { }
}
