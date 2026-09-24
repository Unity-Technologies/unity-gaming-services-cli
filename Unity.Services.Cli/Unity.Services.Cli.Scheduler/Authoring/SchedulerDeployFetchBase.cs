using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.IO;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model;

namespace Unity.Services.Cli.Scheduler.Deploy;

abstract class SchedulerDeployFetchBase
{
    readonly ISchedulerResourceLoader m_ResourceLoader;

    public string ServiceType => SchedulerConstants.ServiceType;
    public string ServiceName => SchedulerConstants.ServiceName;

    public IReadOnlyList<string> FileExtensions => new[]
    {
        SchedulerConstants.DeployFileExtension
    };

    protected SchedulerDeployFetchBase(ISchedulerResourceLoader resourceLoader)
    {
        m_ResourceLoader = resourceLoader;
    }

    protected async Task<(IReadOnlyList<SchedulerEntryDeploymentItem>, IReadOnlyList<SchedulerEntryDeploymentItem>)> GetResourcesFromFiles(
        IReadOnlyCollection<string> filePaths,
        CancellationToken token)
    {
        var resources = (await Task.WhenAll(
                filePaths.Select(filePath => m_ResourceLoader.ReadResource(filePath, token))))
            .SelectMany(resource => resource)
            .ToList();

        var success = resources
            .Where(r => r.Status.MessageSeverity != SeverityLevel.Error)
            .ToList();
        var failed = resources
            .Where(r => r.Status.MessageSeverity == SeverityLevel.Error)
            .ToList();

        return (success, failed);
    }

}
