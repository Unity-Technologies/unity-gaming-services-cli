namespace Unity.Services.Cli.Common.Exceptions;

public static class ExitCode
{
    /// <summary>
    /// Exit code when program succeeds
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// Exit code for known and properly handled errors
    /// </summary>
    public const int HandledError = 1;

    /// <summary>
    /// Exit code for unhandled and unknown errors
    /// </summary>
    public const int UnhandledError = 2;

    /// <summary>
    /// Exit code when the user cancelled the operation.
    /// </summary>
    /// <remarks>
    /// The value is used to match the SIGTERM signal's exit code used in Unix and containers.
    /// </remarks>
    public const int Cancelled = 143;
}
