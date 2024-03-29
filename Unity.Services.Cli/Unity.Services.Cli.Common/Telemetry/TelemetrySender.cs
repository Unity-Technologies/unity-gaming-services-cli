using Unity.Services.TelemetryApi.Generated.Api;
using Unity.Services.TelemetryApi.Generated.Model;

namespace Unity.Services.Cli.Common.Telemetry;

public class TelemetrySender
{
    internal Dictionary<string, string> CommonTags { get; }
    internal Dictionary<string, string> ProductTags { get; }
    internal ITelemetryApi TelemetryApi { get; private set; }

    public TelemetrySender(ITelemetryApi telemetryApi, Dictionary<string, string> commonTags,
        Dictionary<string, string> productTags)
    {
        CommonTags = commonTags;
        ProductTags = productTags;
        TelemetryApi = telemetryApi;
    }

    internal void SendRequest(
        Dictionary<string, string>? metricsCommonTags,
        Dictionary<string, string>? diagnosticsCommonTags,
        List<DiagnosticEvent>? diagnotics,
        List<Metric>? metrics
    )
    {
#if !ENABLE_UGS_CLI_TELEMETRY
        if (TelemetryApi.GetType() == typeof(TelemetryApi.Generated.Api.TelemetryApi))
        {
            return;
        }
#endif
        TelemetryApi.PostRecordWithHttpInfo(new PostRecordRequest(
                CommonTags,
                metricsCommonTags,
                diagnosticsCommonTags,
                diagnotics,
                metrics
            )
        );
    }

    internal void SendDiagnosticsRequest(Dictionary<string, string>? diagnosticsCommonTags,
        List<DiagnosticEvent>? diagnotics)
        => SendRequest(null, diagnosticsCommonTags, diagnotics, null);
}
