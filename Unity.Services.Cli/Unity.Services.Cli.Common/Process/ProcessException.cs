using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.Common.Process;

public class ProcessException : CliException
{
    public ProcessException(string message, Exception? innerException, int exitCode) : base(message, innerException, exitCode)
    {
    }

    public ProcessException(string message, int exitCode = Exceptions.ExitCode.HandledError) : base(message, exitCode)
    {
    }
}
