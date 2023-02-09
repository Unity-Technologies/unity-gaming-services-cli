using System;
using Unity.Services.Cli.Common.SystemEnvironment;

namespace Unity.Services.Cli.Common.Telemetry.AnalyticEvent.AnalyticEventFactory;

public class AnalyticEventFactory : IAnalyticEventFactory
{
    readonly ISystemEnvironmentProvider m_SystemEnvironmentProvider;

    public AnalyticEventFactory(ISystemEnvironmentProvider systemEnvironmentProvider)
    {
        m_SystemEnvironmentProvider = systemEnvironmentProvider;
    }

    public string ProjectId { get; set; } = "";

    public IAnalyticEvent CreateEvent()
    {
        var analyticEvent = new AnalyticEvent(m_SystemEnvironmentProvider);
        analyticEvent.AddData(TagKeys.ProjectId, ProjectId);
        return analyticEvent;
    }
}
