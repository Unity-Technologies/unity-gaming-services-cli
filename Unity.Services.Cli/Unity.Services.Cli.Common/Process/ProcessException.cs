using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.Common.Process;

[Serializable]
public class ProcessException : CliException
{
    protected ProcessException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public ProcessException(string message, Exception? innerException, int exitCode) : base(message, innerException, exitCode)
    {
    }

    public ProcessException(string message, int exitCode = Exceptions.ExitCode.HandledError) : base(message, exitCode)
    {
    }
}
