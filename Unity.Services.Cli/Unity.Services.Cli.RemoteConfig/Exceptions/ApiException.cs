using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.RemoteConfig.Exceptions;

public class ApiException : CliException
{
    public ApiException(string message, Exception? innerException, int exitCode)
        : base(message, innerException, exitCode) { }

    public ApiException(string message, int exitCode)
        : base(message, exitCode) { }

    public ApiException(int exitCode)
        : base(exitCode) { }
}
