using System.CommandLine.Invocation;

namespace Unity.Services.Cli.Common.Telemetry;

/// <summary>
/// Object used to send diagnostic events to the backend.
/// </summary>
public interface IDiagnostics
{
    /// <summary>
    /// Send a diagnostic event to the telemetry service to report unexpected behaviour.
    /// </summary>
    /// <param name="name">
    /// Name of the event.
    /// </param>
    /// <param name="message">
    /// An error message describing what error occured.
    /// </param>
    /// <param name="context">
    /// The invocation context to provide the commands used
    /// </param>
    void SendDiagnostic(string name, string message, InvocationContext context);
}
