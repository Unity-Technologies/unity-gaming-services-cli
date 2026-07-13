using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using Statuses = UnityEditor.Purchasing.Editor.Authoring.Core.Model.Statuses;

namespace Unity.Services.Cli.Purchasing.IO;

class CliCsvCatalogLoader
{
    readonly IFileSystem m_FileSystem;
    readonly ICatalogCsvParser m_CsvParser;

    public CliCsvCatalogLoader(IFileSystem fileSystem, ICatalogCsvParser csvParser)
    {
        m_FileSystem = fileSystem;
        m_CsvParser = csvParser;
    }

    public virtual async Task<(
        List<CatalogEntryDeploymentItem> entries,
        List<IDeploymentItem> failed)> ReadCatalog(
        string path,
        CancellationToken token)
    {
        var entries = new List<CatalogEntryDeploymentItem>();
        var failed = new List<IDeploymentItem>();

        try
        {
            var content = await m_FileSystem.ReadAllText(path, token);
            var catalogItems = m_CsvParser.Parse(content, out var issues);

            foreach (var issue in issues)
            {
                failed.Add(new CatalogEntryDeploymentItem(path)
                {
                    Name = issue.Description,
                    Status = new DeploymentStatus(
                        "Failed to read", issue.Description, SeverityLevel.Error),
                });
            }

            if (catalogItems.Count == 0 && issues.Count > 0)
                return (entries, failed);

            foreach (var catalogItem in catalogItems)
            {
                if (string.IsNullOrEmpty(catalogItem.CatalogListingId))
                    catalogItem.CatalogListingId = CatalogItem.CatalogListingIdPrefix + catalogItem.uSku;

                var ucatPath = Path.Combine(
                    path,
                    catalogItem.uSku + Constants.FileExtension);

                entries.Add(new CatalogEntryDeploymentItem(ucatPath)
                {
                    CatalogItem = catalogItem,
                });
            }
        }
        catch (IOException e)
        {
            failed.Add(new CatalogEntryDeploymentItem(path)
            {
                Status = Statuses.GetFailedToLoad(e, path),
            });
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            failed.Add(new CatalogEntryDeploymentItem(path)
            {
                Status = Statuses.GetFailedToRead(e, path),
            });
        }

        return (entries, failed);
    }

    public virtual async Task WriteCatalog(
        string path,
        List<CatalogItem> items,
        CancellationToken token)
    {
        var content = m_CsvParser.Serialize(items);
        await m_FileSystem.WriteAllText(path, content, token);
    }

    public virtual async Task DeleteCatalog(string path, CancellationToken token)
    {
        await m_FileSystem.Delete(path, token);
    }
}
