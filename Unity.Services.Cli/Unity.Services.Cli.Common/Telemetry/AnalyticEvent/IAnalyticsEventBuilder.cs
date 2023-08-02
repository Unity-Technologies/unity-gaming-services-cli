namespace Unity.Services.Cli.Common.Telemetry.AnalyticEvent;

public interface IAnalyticsEventBuilder
{
    public void SetCommandName(string name);
    public void AddCommandOption(string optionName);
    public void AddAuthoringServiceProcessed(string service);
    public void SetAuthoringCommandlinePathsInputCount(IReadOnlyList<string> filePaths);
    public void SendCommandCompletedEvent();
}
