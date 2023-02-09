using System;

namespace Unity.Services.Cli.Common.Telemetry.AnalyticEvent;

public interface IAnalyticEvent
{
    public void AddData(string key, object value);
    public void Send();
}
