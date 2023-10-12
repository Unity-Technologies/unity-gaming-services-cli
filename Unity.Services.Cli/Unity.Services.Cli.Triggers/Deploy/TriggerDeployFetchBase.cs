using Unity.Services.Cli.Triggers.IO;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Triggers.Deploy;

abstract class TriggerDeployFetchBase
{
    readonly ITriggersResourceLoader m_ResourceLoader;

    protected TriggerDeployFetchBase(ITriggersResourceLoader resourceLoader)
    {
        m_ResourceLoader = resourceLoader;
    }

    protected static void SetFileStatus(IReadOnlyList<TriggersFileItem> deserializedFiles)
    {
        foreach (var deployedFile in deserializedFiles)
        {
            var failedItems = deployedFile.Content.Configs.Count(c => c.Status.MessageSeverity == SeverityLevel.Error);
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
        }
    }

    protected async Task<(IReadOnlyList<TriggersFileItem>,IReadOnlyList<TriggersFileItem>)> GetResourcesFromFiles(
        IReadOnlyCollection<string> filePaths,
        CancellationToken token)
    {
        var resources  = await Task.WhenAll(filePaths.Select(f => m_ResourceLoader.LoadResource(f,token)));
        var deserializedFiles = resources
            .Where(r => r.Status.MessageSeverity != SeverityLevel.Error)
            .ToList();
        var failedToDeserialize = resources
            .Where(r => r.Status.MessageSeverity == SeverityLevel.Error)
            .ToList();
        return (deserializedFiles, failedToDeserialize);
    }
}
