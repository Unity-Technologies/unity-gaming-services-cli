using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.Authoring.Exceptions;

/// <summary>
/// Exception caused by user operation during deploy
/// </summary>
[Serializable]
public class DeployException : CliException
{
    protected DeployException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    public DeployException(int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(exitCode)
    {
    }

    /// <summary>
    /// <see cref="DeployException"/> constructor.
    /// </summary>
    /// <param name="message">A message with instructions to guide user how to fix the operation during deploy.</param>
    /// <param name="exitCode">Exit code when this exception triggered. Default value is HandledError.</param>
    public DeployException(string message, int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(message, exitCode)
    {
    }

    public DeployException(
        string message, Exception innerException, int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(message, innerException, exitCode)
    {
    }
}
