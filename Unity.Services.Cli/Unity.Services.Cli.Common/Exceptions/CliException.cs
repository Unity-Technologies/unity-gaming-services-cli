using System.Runtime.Serialization;

namespace Unity.Services.Cli.Common.Exceptions;

/// <summary>
/// Exception caused by user operation. The exception message should include instructions to fix operation
/// </summary>
[Serializable]
public class CliException : Exception
{
    public int ExitCode { get; }

    protected CliException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    public CliException(string message, Exception? innerException, int exitCode)
        : base(message, innerException)
    {
        ExitCode = exitCode;
    }

    public CliException(string message, int exitCode)
        : this(message, null, exitCode) { }

    public CliException(int exitCode)
        : this("", null, exitCode) { }
}
