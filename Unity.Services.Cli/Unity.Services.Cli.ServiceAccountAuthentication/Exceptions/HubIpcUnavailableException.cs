using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.ServiceAccountAuthentication.Exceptions;

public class HubIpcUnavailableException : CliException
{
    public HubIpcUnavailableException(string message, int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(message, exitCode) { }

    public HubIpcUnavailableException(string message, Exception innerException, int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(message, innerException, exitCode) { }
}
