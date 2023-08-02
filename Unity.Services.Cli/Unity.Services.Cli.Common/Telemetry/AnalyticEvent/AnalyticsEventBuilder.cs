using System.IO.Abstractions;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent.AnalyticEventFactory;

namespace Unity.Services.Cli.Common.Telemetry.AnalyticEvent;

public class AnalyticsEventBuilder : IAnalyticsEventBuilder
{
    readonly IAnalyticEventFactory m_AnalyticEventFactory;
    readonly IFileSystem m_FileSystem;
    internal string Command = "";
    internal readonly List<string> Options = new();
    internal readonly List<string> AuthoringServicesProcessed = new();
    internal int FolderPathsCommandlineInputCount = 0;
    internal int FilePathsCommandlineInputCount = 0;

    public AnalyticsEventBuilder(IAnalyticEventFactory analyticEventFactory, IFileSystem fileSystem)
    {
        m_AnalyticEventFactory = analyticEventFactory;
        m_FileSystem = fileSystem;
    }

    public void SetCommandName(string commandName)
        => Command = commandName;

    public void AddCommandOption(string optionName)
        => Options.Add(optionName);

    public void AddAuthoringServiceProcessed(string service)
        => AuthoringServicesProcessed.Add(service);

    public void SetAuthoringCommandlinePathsInputCount(IReadOnlyList<string> filePaths)
    {
        foreach (var filePath in filePaths)
        {
            var fullPath = m_FileSystem.Path.GetFullPath(filePath);

            if (m_FileSystem.File.Exists(fullPath))
            {
                FilePathsCommandlineInputCount++;
            }
            else if (m_FileSystem.Directory.Exists(fullPath))
            {
                FolderPathsCommandlineInputCount++;
            }
        }
    }

    public void SendCommandCompletedEvent()
    {
        var analyticEvent = m_AnalyticEventFactory.CreateMetricEvent();
        analyticEvent.AddData(TagKeys.Timestamp, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        analyticEvent.AddData(MetricTagKeys.Command, Command);
        analyticEvent.AddData(MetricTagKeys.Options, Options.ToArray());
        analyticEvent.AddData(MetricTagKeys.ServicesProcessed, AuthoringServicesProcessed.ToArray());
        analyticEvent.AddData(MetricTagKeys.NbFilePaths, FilePathsCommandlineInputCount);
        analyticEvent.AddData(MetricTagKeys.NbFolderPaths, FolderPathsCommandlineInputCount);
        analyticEvent.Send();
    }
}
