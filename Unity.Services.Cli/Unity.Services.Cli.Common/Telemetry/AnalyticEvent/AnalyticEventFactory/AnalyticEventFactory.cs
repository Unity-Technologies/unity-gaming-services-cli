using System;
using Unity.Services.Cli.Common.SystemEnvironment;

namespace Unity.Services.Cli.Common.Telemetry.AnalyticEvent.AnalyticEventFactory;

public class AnalyticEventFactory : IAnalyticEventFactory
{

#if ENABLE_UGS_CLI_TELEMETRY
    const string k_DiagnosticsEventName = "ugs.cli.diagnostic.v1";
    const string k_MetricsEventName = "ugs.cli.metric.v3";
#else
    const string k_DiagnosticsEventName = "ugs.cli.diagnostic.v1.stg";
    const string k_MetricsEventName = "ugs.cli.metric.v3.stg";
#endif


    readonly ISystemEnvironmentProvider m_SystemEnvironmentProvider;

    public AnalyticEventFactory(ISystemEnvironmentProvider systemEnvironmentProvider)
    {
        m_SystemEnvironmentProvider = systemEnvironmentProvider;
    }

    public string ProjectId { get; set; } = "";

    public IAnalyticEvent CreateMetricEvent()
    {
        var analyticEvent = new AnalyticEvent(m_SystemEnvironmentProvider, k_MetricsEventName);
        analyticEvent.AddData(TagKeys.ProjectId, ProjectId);
        return analyticEvent;
    }

    public IAnalyticEvent CreateDiagnosticEvent()
    {
        var analyticEvent = new AnalyticEvent(m_SystemEnvironmentProvider, k_DiagnosticsEventName);
        analyticEvent.AddData(TagKeys.ProjectId, ProjectId);
        return analyticEvent;
    }
}
