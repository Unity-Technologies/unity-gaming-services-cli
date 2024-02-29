using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Scheduler.Deploy;

abstract class SchedulerDeployFetchBase
{
    readonly IScheduleResourceLoader m_ResourceLoader;

    public string ServiceType => SchedulerConstants.ServiceType;
    public string ServiceName => SchedulerConstants.ServiceName;

    public IReadOnlyList<string> FileExtensions => new[]
    {
        SchedulerConstants.DeployFileExtension
    };

    protected SchedulerDeployFetchBase(IScheduleResourceLoader resourceLoader)
    {
        m_ResourceLoader = resourceLoader;
    }

    protected static void SetFileStatus(IReadOnlyList<ScheduleFileItem> deserializedFiles)
    {
        foreach (var deployedFile in deserializedFiles)
        {
            var failedItems = deployedFile.Content.Configs.Count(c => c.Value.Status.MessageSeverity == SeverityLevel.Error);
            if (failedItems == deployedFile.Content.Configs.Count)
            {
                deployedFile.SetStatusSeverity(SeverityLevel.Error);
                deployedFile.SetStatusDescription("Failed to deploy");
                deployedFile.SetStatusDetail("All items failed to deploy");
            }
            else if (failedItems > 0)
            {
                deployedFile.SetStatusSeverity(SeverityLevel.Warning);
                deployedFile.SetStatusDescription("Partial deployment");
                deployedFile.SetStatusDetail("Some items failed to deploy");
            }
            else
            {
                deployedFile.SetStatusSeverity(SeverityLevel.Success);
                deployedFile.SetStatusDescription("Deployed");
                deployedFile.SetStatusDetail("All items successfully deployed");
            }
        }
    }

    protected async Task<(IReadOnlyList<ScheduleFileItem>,IReadOnlyList<ScheduleFileItem>)> GetResourcesFromFiles(
        IReadOnlyCollection<string> filePaths,
        CancellationToken token)
    {
        var resources  = await Task.WhenAll(
            filePaths.Select(f => m_ResourceLoader.LoadResource(f,token)));
        var deserializedFiles = resources
            .Where(r => r.Status.MessageSeverity != SeverityLevel.Error)
            .ToList();
        var failedToDeserialize = resources
            .Where(r => r.Status.MessageSeverity == SeverityLevel.Error)
            .ToList();
        return (deserializedFiles, failedToDeserialize);
    }
}
