using System.CommandLine.Invocation;
using System.Text;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.TelemetryApi.Generated.Model;

namespace Unity.Services.Cli.Common.Telemetry;

public class Diagnostics : IDiagnostics
{
    const int k_MaxDiagnosticMessageLength = 10000;
    const string k_DiagnosticMessageTruncateSuffix = "[truncated]";
    readonly TelemetrySender m_TelemetrySender;
    readonly ISystemEnvironmentProvider m_EnvironmentProvider;

    public Diagnostics(TelemetrySender telemetrySender, ISystemEnvironmentProvider systemEnvironmentProvider)
    {
        m_TelemetrySender = telemetrySender;
        m_EnvironmentProvider = systemEnvironmentProvider;
    }

    public void SendDiagnostic(string name, string message, InvocationContext context)
    {
        if (TelemetryConfigurationProvider.IsTelemetryDisabled(m_EnvironmentProvider))
            return;

        var diagnosticList = CreateDiagnosticList(name, message, context);

        m_TelemetrySender.SendDiagnosticsRequest(null, diagnosticList);
    }

    List<DiagnosticEvent> CreateDiagnosticList(string name, string message, InvocationContext context)
    {
        var diagnosticList = new List<DiagnosticEvent>();
        var diagnostic = new DiagnosticEvent
        {
            Content = new Dictionary<string, string>(m_TelemetrySender.ProductTags)
        };

        diagnostic.Content.Add(TagKeys.DiagnosticName, name);
        if (message.Length > k_MaxDiagnosticMessageLength)
        {
            message = $"{message.Substring(0, k_MaxDiagnosticMessageLength)}{Environment.NewLine}{k_DiagnosticMessageTruncateSuffix}";
        }
        diagnostic.Content.Add(TagKeys.DiagnosticMessage, message);

        var command = new StringBuilder("ugs");
        foreach (var arg in context.ParseResult.Tokens)
        {
            command.Append(" " + arg);
        }
        diagnostic.Content.Add(TagKeys.Command, command.ToString());

        diagnosticList.Add(diagnostic);
        return diagnosticList;
    }
}
