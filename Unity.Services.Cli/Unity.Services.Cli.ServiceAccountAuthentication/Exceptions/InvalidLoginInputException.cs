using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.ServiceAccountAuthentication.Exceptions;

public class InvalidLoginInputException : CliException
{
    public InvalidLoginInputException(int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(exitCode) { }

    public InvalidLoginInputException(string message, int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(message, exitCode) { }

    public InvalidLoginInputException(string message, Exception innerException, int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(message, innerException, exitCode) { }
}
