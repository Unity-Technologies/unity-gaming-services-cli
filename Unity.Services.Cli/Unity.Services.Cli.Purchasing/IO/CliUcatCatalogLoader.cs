using Newtonsoft.Json;
using Unity.Services.Cli.Purchasing.Authoring;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.Purchasing.IO;

class CliUcatCatalogLoader : ICatalogLoader
{
    readonly IFileSystem m_FileSystem;
    readonly JsonSerializerSettings m_SerializerSettings;

    public CliUcatCatalogLoader(IFileSystem fileSystem)
    {
        m_FileSystem = fileSystem;
        m_SerializerSettings = CatalogItemConfigFile.GetSerializationSettings();
    }

    public async Task<CatalogEntryDeploymentItem> ReadCatalog(string path, CancellationToken token)
    {
        var deploymentItem = new CatalogEntryDeploymentItem(path);
        try
        {
            var text = await m_FileSystem.ReadAllText(path, token);
            var model = JsonConvert.DeserializeObject<CatalogItem>(text, m_SerializerSettings);
            if (model == null)
                throw new JsonException("Deserialized catalog item is null.");
            deploymentItem.CatalogItem = model;
            var stem = Path.GetFileNameWithoutExtension(path);
            model.CatalogListingId = CatalogItem.CatalogListingIdPrefix + stem;
            if (model.uSku == null)
                model.uSku = stem;
        }
        catch (IOException e)
        {
            deploymentItem.Status = Statuses.GetFailedToLoad(e, deploymentItem.Path);
        }
        catch (JsonException e)
        {
            deploymentItem.Status = Statuses.GetFailedToRead(e, deploymentItem.Path);
        }

        return deploymentItem;
    }

    public async Task CreateOrUpdateCatalog(
        CatalogEntryDeploymentItem deployableEntryDeploymentItem,
        CancellationToken token)
    {
        var fileName = Path.GetFileNameWithoutExtension(deployableEntryDeploymentItem.Path);
        try
        {
            var item = deployableEntryDeploymentItem.CatalogItem;
            var shouldStripSku = item.uSku == fileName;
            string? originalSku = null;
            if (shouldStripSku)
            {
                originalSku = item.uSku;
                item.uSku = null;
            }

            var text = JsonConvert.SerializeObject(item, m_SerializerSettings);

            if (shouldStripSku)
                item.uSku = originalSku;

            await m_FileSystem.WriteAllText(deployableEntryDeploymentItem.Path, text, token);
        }
        catch (JsonException e)
        {
            deployableEntryDeploymentItem.Status = Statuses.GetFailedToSerialize(
                e, deployableEntryDeploymentItem.Path);
        }
        catch (Exception e)
        {
            deployableEntryDeploymentItem.Status = Statuses.GetFailedToWrite(
                e, deployableEntryDeploymentItem.Path);
        }
    }

    public async Task DeleteCatalog(
        CatalogEntryDeploymentItem entryDeploymentItem,
        CancellationToken token)
    {
        try
        {
            await m_FileSystem.Delete(entryDeploymentItem.Path, token);
        }
        catch (IOException e)
        {
            entryDeploymentItem.Status = Statuses.GetFailedToDelete(e, entryDeploymentItem.Path);
        }
    }

    public void DeserializeAndPopulateFromPath(CatalogEntryDeploymentItem config, string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path), "cannot deserialize a config with an empty path.");

        var content = m_FileSystem.ReadAllText(path, CancellationToken.None).GetAwaiter().GetResult();
        var catalogItem = JsonConvert.DeserializeObject<CatalogItem>(content, m_SerializerSettings)
            ?? throw new JsonException("Deserialized catalog item is null.");
        var stem = Path.GetFileNameWithoutExtension(path);
        catalogItem.CatalogListingId = CatalogItem.CatalogListingIdPrefix + stem;
        if (string.IsNullOrEmpty(catalogItem.uSku))
            catalogItem.uSku = stem;

        config.CatalogItem = catalogItem;
    }
}
