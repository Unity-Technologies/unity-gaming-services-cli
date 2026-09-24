using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;
using Statuses = UnityEditor.Purchasing.Editor.Authoring.Core.Model.Statuses;

namespace Unity.Services.Cli.Purchasing.Authoring;

abstract class PurchasingBaseService
{
    protected readonly ILiveContentConfigClient m_Client;
    protected readonly ICatalogUcatLoader m_UcatCatalogLoader;
    protected readonly ICatalogCsvLoader m_CsvCatalogLoader;

    protected PurchasingBaseService(
        ILiveContentConfigClient client,
        ICatalogUcatLoader ucatCatalogLoader,
        ICatalogCsvLoader csvCatalogLoader)
    {
        m_Client = client;
        m_UcatCatalogLoader = ucatCatalogLoader;
        m_CsvCatalogLoader = csvCatalogLoader;
    }

    public string ServiceType => "Purchasing";
    public string ServiceName => "purchasing";

    static readonly string[] k_FileExtensions = { Constants.FileExtension };
    public virtual IReadOnlyList<string> FileExtensions => k_FileExtensions;

    protected static IReadOnlyList<string> FilterByExtension(
        IReadOnlyList<string> filePaths, string extension)
    {
        return filePaths
            .Where(p => p.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    protected async Task<(
        List<CatalogEntryDeploymentItem> entries,
        List<IDeploymentItem> failedToLoad)> LoadUcatFiles(
        IReadOnlyList<string> ucatPaths,
        CancellationToken cancellationToken)
    {
        var entries = new List<CatalogEntryDeploymentItem>();
        var failedToLoad = new List<IDeploymentItem>();

        var ucatTasks = ucatPaths.Select(f => m_UcatCatalogLoader.ReadCatalog(f, cancellationToken));
        var loadedUcats = await Task.WhenAll(ucatTasks);

        foreach (var item in loadedUcats)
        {
            if (item.Status.MessageSeverity == SeverityLevel.Error)
            {
                failedToLoad.Add(item);
            }
            else
            {
                entries.Add(item);
            }
        }

        return (entries, failedToLoad);
    }

    protected async Task<(List<CatalogEntryDeploymentItem> entries, List<IDeploymentItem> failed)> LoadCsvFiles(
        IReadOnlyList<string> csvPaths,
        CancellationToken cancellationToken)
    {
        var entries = new List<CatalogEntryDeploymentItem>();
        var failed = new List<IDeploymentItem>();

        var csvTasks = csvPaths.Select(f => LoadCsvFileEntries(f, cancellationToken));
        var results = await Task.WhenAll(csvTasks);

        foreach (var (csvEntries, csvFailed) in results)
        {
            entries.AddRange(csvEntries);
            failed.AddRange(csvFailed);
        }

        return (entries, failed);
    }

    async Task<(List<CatalogEntryDeploymentItem> entries, List<IDeploymentItem> failed)> LoadCsvFileEntries(
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            var (catalogItems, issues) = await m_CsvCatalogLoader.ReadCatalog(path, cancellationToken);
            var failed = MapIssuesToFailedItems(path, issues);

            if (catalogItems.Count == 0 && issues.Count > 0)
            {
                return (new List<CatalogEntryDeploymentItem>(), failed);
            }

            var entries = ConvertToDeploymentItems(path, catalogItems);
            return (entries, failed);
        }
        catch (IOException e)
        {
            return (new List<CatalogEntryDeploymentItem>(),
                new List<IDeploymentItem>
                {
                    new CatalogEntryDeploymentItem(path)
                    {
                        Status = Statuses.GetFailedToLoad(e, path),
                    },
                });
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return (new List<CatalogEntryDeploymentItem>(),
                new List<IDeploymentItem>
                {
                    new CatalogEntryDeploymentItem(path)
                    {
                        Status = Statuses.GetFailedToRead(e, path),
                    },
                });
        }
    }

    static List<IDeploymentItem> MapIssuesToFailedItems(string path, List<AssetState> issues)
    {
        return issues.Select(issue => (IDeploymentItem)new CatalogEntryDeploymentItem(path)
        {
            Name = issue.Description,
            Status = new DeploymentStatus(issue.Description, issue.Detail, issue.Level),
        }).ToList();
    }

    static List<CatalogEntryDeploymentItem> ConvertToDeploymentItems(string path, List<CatalogItem> catalogItems)
    {
        var entries = new List<CatalogEntryDeploymentItem>(catalogItems.Count);
        foreach (var item in catalogItems)
        {
            if (string.IsNullOrEmpty(item.CatalogListingId))
            {
                item.CatalogListingId = CatalogItem.CatalogListingIdPrefix + item.uSku;
            }

            entries.Add(new CatalogEntryDeploymentItem(Path.Combine(path, item.uSku + Constants.FileExtension))
            {
                CatalogItem = item,
            });
        }
        return entries;
    }

    protected static IReadOnlyList<IDeploymentItem> GetItemsByAction(
        IReadOnlyList<IDeploymentItem> source, string action)
    {
        return source.Where(f =>
            f.Status.MessageSeverity == SeverityLevel.Success
            && (f.Status.MessageDetail?.StartsWith(action) ?? false)).ToList();
    }
}
