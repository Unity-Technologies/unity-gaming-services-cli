using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Authoring.Utils;
using Unity.Services.Cli.Purchasing.IO;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;
using Statuses = UnityEditor.Purchasing.Editor.Authoring.Core.Model.Statuses;

namespace Unity.Services.Cli.Purchasing.Authoring;

class PurchasingFetchService : PurchasingBaseService, IFetchService
{
    static readonly string[] k_FileExtensions = { Constants.FileExtension, Constants.CsvFileExtension };
    public override IReadOnlyList<string> FileExtensions => k_FileExtensions;

    readonly CliCsvCatalogLoader m_CsvCatalogLoader;

    public PurchasingFetchService(
        ILiveContentConfigClient client,
        ICatalogLoader catalogLoader,
        CliCsvCatalogLoader csvCatalogLoader)
        : base(client, catalogLoader)
    {
        m_CsvCatalogLoader = csvCatalogLoader;
    }

    public async Task<FetchResult> FetchAsync(
        FetchInput input,
        IReadOnlyList<AuthoringFile> authoringFiles,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        await m_Client.Initialize(environmentId, projectId, cancellationToken);

        loadingContext?.Status("Reading local Purchasing files...");
        var localPaths = authoringFiles.ToPaths();
        var (localEntries, failedToLoad) =
            await LoadUcatFiles(localPaths, cancellationToken);
        var (csvEntries, csvFailed) =
            await LoadCsvFiles(localPaths, m_CsvCatalogLoader, cancellationToken);
        localEntries.AddRange(csvEntries);
        failedToLoad.AddRange(csvFailed);

        var (filteredEntries, duplicateItems) = FilterDuplicates(localEntries);

        loadingContext?.Status("Fetching remote Purchasing catalog...");
        List<CatalogItem> remoteItems;
        try
        {
            remoteItems = await m_Client.List(cancellationToken);
        }
        catch (ClientException e)
        {
            foreach (var entry in filteredEntries)
            {
                entry.Status = Statuses.GetFailedToFetch(e.Message);
            }

            var allFailed = new List<IDeploymentItem>();
            allFailed.AddRange(filteredEntries);
            allFailed.AddRange(failedToLoad);
            allFailed.AddRange(duplicateItems);
            return new PurchasingFetchResult(allFailed, input.DryRun);
        }

        var remoteMap = new Dictionary<string, CatalogItem>();
        foreach (var ri in remoteItems)
        {
            if (!string.IsNullOrEmpty(ri.CatalogListingId))
                remoteMap[ri.CatalogListingId] = ri;
        }

        // Built from localEntries (pre-dedup) so duplicates still count as "local" during reconcile
        var localIds = new HashSet<string>();
        foreach (var entry in localEntries)
        {
            if (entry.CatalogItem?.CatalogListingId != null)
                localIds.Add(entry.CatalogItem.CatalogListingId);
        }

        var allItems = new List<IDeploymentItem>();
        allItems.AddRange(failedToLoad);
        allItems.AddRange(duplicateItems);

        var csvErrors = BuildCsvErrors(failedToLoad, duplicateItems);

        loadingContext?.Status("Applying fetched Purchasing data...");
        await ApplyRemoteData(input, filteredEntries, remoteMap, allItems, csvErrors, cancellationToken);

        if (input.Reconcile)
            await ReconcileRemoteOnly(input, remoteMap, localIds, allItems, cancellationToken);

        return new PurchasingFetchResult(allItems, input.DryRun);
    }

    async Task ApplyRemoteData(
        FetchInput input,
        List<CatalogEntryDeploymentItem> filteredEntries,
        Dictionary<string, CatalogItem> remoteMap,
        List<IDeploymentItem> allItems,
        CsvErrorPaths csvErrors,
        CancellationToken cancellationToken)
    {
        var modifiedCsvPaths = new HashSet<string>();

        foreach (var entry in filteredEntries)
        {
            var id = entry.CatalogItem?.CatalogListingId;
            if (id != null && remoteMap.TryGetValue(id, out var remote))
            {
                entry.CatalogItem = remote;
                entry.CatalogItem.CatalogListingId = id;

                if (IsCsvEntry(entry))
                {
                    modifiedCsvPaths.Add(GetCsvPath(entry));
                }
                else if (!input.DryRun)
                {
                    try
                    {
                        await m_UcatCatalogLoader.CreateOrUpdateCatalog(entry, cancellationToken);
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        entry.Status = Statuses.GetFailedToFetch(e.Message);
                        allItems.Add(entry);
                        continue;
                    }
                }

                if (entry.Status.MessageSeverity != SeverityLevel.Error)
                {
                    entry.Status = Statuses.GetFetched(Constants.Updated);
                    entry.Progress = 100f;
                }
            }
            else if (input.Reconcile)
            {
                if (IsCsvEntry(entry))
                {
                    modifiedCsvPaths.Add(GetCsvPath(entry));
                }
                else if (!input.DryRun)
                {
                    try
                    {
                        await m_UcatCatalogLoader.DeleteCatalog(entry, cancellationToken);
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        entry.Status = Statuses.GetFailedToFetch(e.Message);
                        allItems.Add(entry);
                        continue;
                    }
                }

                if (entry.Status.MessageSeverity != SeverityLevel.Error)
                {
                    entry.Status = Statuses.GetFetched(Constants.Deleted);
                    entry.Progress = 100f;
                }
            }
            allItems.Add(entry);
        }

        if (!input.DryRun)
        {
            modifiedCsvPaths.ExceptWith(csvErrors.All);
            await WriteCsvFiles(modifiedCsvPaths, filteredEntries, allItems, cancellationToken);
        }

        foreach (var entry in filteredEntries.Where(en => IsCsvEntry(en) && csvErrors.All.Contains(GetCsvPath(en))))
        {
            var csvPath = GetCsvPath(entry);
            var reason = csvErrors.Duplicates.Contains(csvPath)
                ? "CSV contains duplicate catalog listing IDs"
                : "CSV file has parse errors";
            entry.Status = Statuses.GetFailedToFetch(
                $"{reason}, cannot update '{csvPath}'");
        }
    }

    async Task ReconcileRemoteOnly(
        FetchInput input,
        Dictionary<string, CatalogItem> remoteMap,
        HashSet<string> localIds,
        List<IDeploymentItem> allItems,
        CancellationToken cancellationToken)
    {
        var rootDir = HasKnownExtension(input.Path)
            ? Path.GetDirectoryName(input.Path) ?? input.Path
            : input.Path;
        foreach (var kvp in remoteMap)
        {
            if (!localIds.Contains(kvp.Key))
            {
                var stem = kvp.Key.StartsWith(CatalogItem.CatalogListingIdPrefix)
                    ? kvp.Key[CatalogItem.CatalogListingIdPrefix.Length..]
                    : kvp.Key;
                var newPath = Path.Combine(rootDir, stem + Constants.FileExtension);
                var newItem = new CatalogEntryDeploymentItem(newPath)
                {
                    CatalogItem = kvp.Value,
                };
                newItem.CatalogItem.CatalogListingId = kvp.Key;

                if (!input.DryRun)
                {
                    try
                    {
                        await m_UcatCatalogLoader.CreateOrUpdateCatalog(newItem, cancellationToken);
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        newItem.Status = Statuses.GetFailedToFetch(e.Message);
                        allItems.Add(newItem);
                        continue;
                    }
                }
                if (newItem.Status.MessageSeverity != SeverityLevel.Error)
                {
                    newItem.Status = Statuses.GetFetched(Constants.Created);
                    newItem.Progress = 100f;
                }
                allItems.Add(newItem);
            }
        }
    }

    async Task WriteCsvFiles(
        HashSet<string> modifiedCsvPaths,
        List<CatalogEntryDeploymentItem> filteredEntries,
        List<IDeploymentItem> allItems,
        CancellationToken cancellationToken)
    {
        foreach (var csvPath in modifiedCsvPaths)
        {
            var itemsToWrite = filteredEntries
                .Where(e => IsCsvEntry(e)
                    && GetCsvPath(e) == csvPath
                    && e.Status.MessageSeverity != SeverityLevel.Error
                    && e.Status.MessageDetail != Constants.Deleted)
                .Select(e => e.CatalogItem)
                .ToList();

            try
            {
                if (itemsToWrite.Count > 0)
                    await m_CsvCatalogLoader.WriteCatalog(csvPath, itemsToWrite, cancellationToken);
                else
                    await m_CsvCatalogLoader.DeleteCatalog(csvPath, cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                foreach (var entry in filteredEntries.Where(en => IsCsvEntry(en) && GetCsvPath(en) == csvPath))
                {
                    entry.Status = Statuses.GetFailedToFetch(e.Message);
                    if (!allItems.Contains(entry))
                        allItems.Add(entry);
                }
            }
        }
    }

    static bool IsCsvEntry(CatalogEntryDeploymentItem entry)
    {
        var dir = Path.GetDirectoryName(entry.Path) ?? string.Empty;
        return dir.EndsWith(Constants.CsvFileExtension, StringComparison.OrdinalIgnoreCase);
    }

    static string GetCsvPath(CatalogEntryDeploymentItem entry)
    {
        return Path.GetDirectoryName(entry.Path) ?? string.Empty;
    }

    record CsvErrorPaths(HashSet<string> All, HashSet<string> Duplicates);

    static CsvErrorPaths BuildCsvErrors(
        List<IDeploymentItem> failedToLoad,
        List<IDeploymentItem> duplicateItems)
    {
        var parseErrors = new HashSet<string>(
            failedToLoad
                .Where(f => f.Path.EndsWith(Constants.CsvFileExtension, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path));

        var duplicates = new HashSet<string>(
            duplicateItems.OfType<CatalogEntryDeploymentItem>()
                .Where(IsCsvEntry)
                .Select(GetCsvPath));

        var all = new HashSet<string>(parseErrors);
        all.UnionWith(duplicates);

        return new CsvErrorPaths(all, duplicates);
    }

    static bool HasKnownExtension(string path)
    {
        return path.EndsWith(Constants.FileExtension, StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(Constants.CsvFileExtension, StringComparison.OrdinalIgnoreCase);
    }

    static (List<CatalogEntryDeploymentItem> filtered, List<IDeploymentItem> duplicates) FilterDuplicates(
        List<CatalogEntryDeploymentItem> entries)
    {
        var groups = entries
            .Where(e => !string.IsNullOrEmpty(e.CatalogItem?.CatalogListingId))
            .GroupBy(e => e.CatalogItem!.CatalogListingId)
            .Where(g => g.Count() > 1)
            .ToList();

        var duplicateIds = new HashSet<string>(groups.Select(g => g.Key));
        var duplicates = new List<IDeploymentItem>();

        foreach (var group in groups)
        {
            foreach (var item in group)
            {
                var others = group.Where(i => i != item).Select(i => $"'{i.Path}'");
                var detail = $"Duplicate catalog listing ID with {string.Join(", ", others)}";
                item.Status = Statuses.GetFailedToFetch(detail);
                duplicates.Add(item);
            }
        }

        var filtered = entries
            .Where(e => !duplicateIds.Contains(e.CatalogItem?.CatalogListingId ?? ""))
            .ToList();

        return (filtered, duplicates);
    }

    class PurchasingFetchResult : FetchResult
    {
        public PurchasingFetchResult(IReadOnlyList<IDeploymentItem> authored, bool dryRun)
            : base(
                GetItemsOfType(authored, Constants.Updated),
                GetItemsOfType(authored, Constants.Deleted),
                GetItemsOfType(authored, Constants.Created),
                GetItemsOfType(authored, string.Empty),
                authored.Where(a => a.Status.MessageSeverity == SeverityLevel.Error).ToList(),
                dryRun)
        {
        }
    }
}
