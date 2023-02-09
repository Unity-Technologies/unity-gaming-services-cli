using System;

namespace Unity.Services.Cli.Common.Telemetry.AnalyticEvent.AnalyticEventFactory;

public interface IAnalyticEventFactory
{
    string ProjectId { get; set; }

    IAnalyticEvent CreateEvent();
}
