using Unity.Analytics.Sender;
using Unity.Services.Cli.Common.SystemEnvironment;

namespace Unity.Services.Cli.Common.Telemetry.AnalyticEvent;

public class AnalyticEvent : AnalyticEventBase, IAnalyticEvent
{
#if ENABLE_UGS_CLI_TELEMETRY
    const string k_MetricsName = "ugs.cli.metric.v3";
#else
    const string k_MetricsName = "ugs.cli.metric.v3.stg";
#endif

    readonly ISystemEnvironmentProvider m_SystemEnvironmentProvider;

    bool IsDisabled => TelemetryConfigurationProvider.IsTelemetryDisabled(m_SystemEnvironmentProvider);

    public AnalyticEvent(ISystemEnvironmentProvider environmentProvider) : base(k_MetricsName)
    {
        m_SystemEnvironmentProvider = environmentProvider;
        AddData(TagKeys.CicdPlatform, TelemetryConfigurationProvider.GetCicdPlatform(environmentProvider!));
        AddData(TagKeys.CliVersion, TelemetryConfigurationProvider.GetCliVersion());
        AddData(TagKeys.OperatingSystem, Environment.OSVersion.ToString());
        AddData(TagKeys.Platform, TelemetryConfigurationProvider.GetOsPlatform());
    }

    public new void AddData(string key, object value)
    {
        base.AddData(key, value);
    }

    public override void Send()
    {
        if (IsDisabled)
            return;

        try
        {
            base.Send();
        }
        catch
        {
            // Metrics should fail silently as to not halt the execution of the application
        }
    }
}
