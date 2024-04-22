using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.CloudContentDelivery.Exceptions;

/// <summary>
/// Example of custom exception for incorrect user operation.
/// </summary>
public class CloudContentDeliveryException : CliException
{
    public CloudContentDeliveryException(int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(exitCode) { }

    /// <summary>
    /// <see cref="CloudContentDeliveryException"/> constructor.
    /// </summary>
    /// <param name="message">A message with instructions to guide user how to fix the operation.</param>
    /// <param name="exitCode">Exit code when this exception triggered. Default value is HandledError.</param>
    public CloudContentDeliveryException(string message, int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(message, exitCode) { }

    public CloudContentDeliveryException(
        string message, Exception innerException, int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base(message, innerException, exitCode) { }
}
