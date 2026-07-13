using Unity.Services.Cli.Purchasing.IO;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;

namespace Unity.Services.Cli.Purchasing.Authoring;

abstract class PurchasingBaseService
{
    protected readonly ILiveContentConfigClient m_Client;
    protected readonly ICatalogLoader m_UcatCatalogLoader;

    protected PurchasingBaseService(
        ILiveContentConfigClient client,
        ICatalogLoader catalogLoader)
    {
        m_Client = client;
        m_UcatCatalogLoader = catalogLoader;
    }

    public string ServiceType => "Purchasing";
    public string ServiceName => "purchasing";

    static readonly string[] k_FileExtensions = { Constants.FileExtension };
    public virtual IReadOnlyList<string> FileExtensions => k_FileExtensions;

    protected async Task<(
        List<CatalogEntryDeploymentItem> entries,
        List<IDeploymentItem> failedToLoad)> LoadUcatFiles(
        IReadOnlyList<string> filePaths,
        CancellationToken cancellationToken)
    {
        var entries = new List<CatalogEntryDeploymentItem>();
        var failedToLoad = new List<IDeploymentItem>();

        var ucatFiles = filePaths
            .Where(p => p.EndsWith(Constants.FileExtension, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var ucatTasks = ucatFiles.Select(f => m_UcatCatalogLoader.ReadCatalog(f, cancellationToken));
        var loadedUcats = await Task.WhenAll(ucatTasks);

        foreach (var item in loadedUcats)
        {
            if (item.Status.MessageSeverity == SeverityLevel.Error)
                failedToLoad.Add(item);
            else
                entries.Add(item);
        }

        return (entries, failedToLoad);
    }

    protected static async Task<(List<CatalogEntryDeploymentItem> entries, List<IDeploymentItem> failed)> LoadCsvFiles(
        IReadOnlyList<string> filePaths,
        CliCsvCatalogLoader csvCatalogLoader,
        CancellationToken cancellationToken)
    {
        var entries = new List<CatalogEntryDeploymentItem>();
        var failed = new List<IDeploymentItem>();

        var csvFiles = filePaths
            .Where(p => p.EndsWith(Constants.CsvFileExtension, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var csvTasks = csvFiles.Select(f => csvCatalogLoader.ReadCatalog(f, cancellationToken));
        var results = await Task.WhenAll(csvTasks);

        foreach (var (csvEntries, csvFailed) in results)
        {
            entries.AddRange(csvEntries);
            failed.AddRange(csvFailed);
        }

        return (entries, failed);
    }

    protected static IReadOnlyList<IDeploymentItem> GetItemsOfType(
        IReadOnlyList<IDeploymentItem> source, string action)
    {
        return source.Where(f =>
            f.Status.MessageSeverity == SeverityLevel.Success
            && (f.Status.MessageDetail?.StartsWith(action) ?? false)).ToList();
    }
}
