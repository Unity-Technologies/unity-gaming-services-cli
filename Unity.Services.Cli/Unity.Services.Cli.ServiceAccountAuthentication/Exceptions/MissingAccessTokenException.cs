using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.ServiceAccountAuthentication.Exceptions;

public class MissingAccessTokenException : CliException
{
    public MissingAccessTokenException(int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(exitCode) { }

    public MissingAccessTokenException(string message, int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(message, exitCode) { }

    public MissingAccessTokenException(string message, Exception innerException, int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(message, innerException, exitCode) { }
}
