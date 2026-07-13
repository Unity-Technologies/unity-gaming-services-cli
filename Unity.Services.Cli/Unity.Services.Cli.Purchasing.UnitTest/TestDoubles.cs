using Unity.Services.Cli.Purchasing.IO;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Deploy;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;
using CoreLogger = UnityEditor.Purchasing.Editor.Authoring.Core.Logger;

namespace Unity.Services.Cli.Purchasing.UnitTest;

/// <summary>
/// In-memory fake for ILiveContentConfigClient. NuGet package interfaces are internal and
/// cannot be proxied by Moq/Castle, so we use concrete test doubles instead.
/// </summary>
class FakeClient : ILiveContentConfigClient
{
    public string? LastEnvironmentId;
    public string? LastProjectId;
    public int InitializeCallCount;
    public List<CatalogItem> RemoteItems = new();
    public List<CatalogItem> UpsertedItems = new();
    public List<CatalogItem> DeletedItems = new();
    public Exception? ListException;

    public Task Initialize(
        string environmentId,
        string projectId,
        CancellationToken cancellationToken)
    {
        InitializeCallCount++;
        LastEnvironmentId = environmentId;
        LastProjectId = projectId;
        return Task.CompletedTask;
    }

    public Task<List<CatalogItem>> List(CancellationToken cancellationToken)
    {
        if (ListException != null)
            throw ListException;
        return Task.FromResult(RemoteItems);
    }

    public Task Upsert(CatalogItem catalogItem, CancellationToken cancellationToken)
    {
        UpsertedItems.Add(catalogItem);
        return Task.CompletedTask;
    }

    public Task Delete(CatalogItem catalogItem, CancellationToken cancellationToken)
    {
        DeletedItems.Add(catalogItem);
        return Task.CompletedTask;
    }
}

class FakeUcatCatalogLoader : ICatalogLoader
{
    public Func<string, CancellationToken, Task<CatalogEntryDeploymentItem>> ReadCatalogImpl =
        (path, _) => Task.FromResult(new CatalogEntryDeploymentItem(path));

    public int ReadCatalogCallCount;
    public List<string> ReadCatalogPaths = new();
    public List<CatalogEntryDeploymentItem> CreateOrUpdateCalls = new();
    public List<CatalogEntryDeploymentItem> DeleteCatalogCalls = new();
    public Exception? CreateOrUpdateException;
    public Exception? DeleteCatalogException;

    public Task<CatalogEntryDeploymentItem> ReadCatalog(string path, CancellationToken token)
    {
        ReadCatalogCallCount++;
        ReadCatalogPaths.Add(path);
        return ReadCatalogImpl(path, token);
    }

    public Task CreateOrUpdateCatalog(
        CatalogEntryDeploymentItem deployableEntryDeploymentItem,
        CancellationToken token)
    {
        if (CreateOrUpdateException != null)
        {
            throw CreateOrUpdateException;
        }

        CreateOrUpdateCalls.Add(deployableEntryDeploymentItem);
        return Task.CompletedTask;
    }

    public Task DeleteCatalog(
        CatalogEntryDeploymentItem entryDeploymentItem,
        CancellationToken token)
    {
        if (DeleteCatalogException != null)
        {
            throw DeleteCatalogException;
        }
        DeleteCatalogCalls.Add(entryDeploymentItem);
        return Task.CompletedTask;
    }

    public void DeserializeAndPopulateFromPath(CatalogEntryDeploymentItem config, string path)
    {
    }
}

class FakeDeploymentHandler : ICatalogDeploymentHandler
{
    public int DeployCallCount;
    public IReadOnlyList<CatalogEntryDeploymentItem>? LastEntries;
    public bool LastDryRun;
    public bool LastReconcile;
    public Exception? DeployException;

    public Task<DeployResult> DeployAsync(
        IReadOnlyList<CatalogEntryDeploymentItem> localResources,
        bool dryRun,
        bool reconcile,
        CancellationToken token)
    {
        DeployCallCount++;
        LastEntries = localResources;
        LastDryRun = dryRun;
        LastReconcile = reconcile;
        if (DeployException != null)
            throw DeployException;
        var result = new DeployResult();
        result.Deployed = localResources.Cast<Unity.Services.DeploymentApi.Editor.IDeploymentItem>().ToList();
        return Task.FromResult(result);
    }
}

class FakeFileSystem : IFileSystem
{
    public Dictionary<string, string> Files = new();
    public List<string> WrittenPaths = new();
    public Dictionary<string, string> WrittenContents = new();
    public List<string> DeletedPaths = new();
    public Exception? ReadException;
    public Exception? DeleteException;

    public Task<string> ReadAllText(string path, CancellationToken token)
    {
        if (ReadException != null)
            throw ReadException;
        if (Files.TryGetValue(path, out var content))
            return Task.FromResult(content);
        throw new IOException($"File not found: {path}");
    }

    public Task WriteAllText(string path, string contents, CancellationToken token)
    {
        WrittenPaths.Add(path);
        WrittenContents[path] = contents;
        Files[path] = contents;
        return Task.CompletedTask;
    }

    public Task Delete(string path, CancellationToken token)
    {
        if (DeleteException != null)
            throw DeleteException;
        DeletedPaths.Add(path);
        Files.Remove(path);
        return Task.CompletedTask;
    }
}

class FakeCatalogCsvParser : ICatalogCsvParser
{
    public Func<string, (List<CatalogItem> items, List<AssetState> issues)> ParseImpl =
        _ => (new List<CatalogItem>(), new List<AssetState>());

    public int ParseCallCount;
    public Exception? ParseException = null;

    public List<CatalogItem> Parse(string csvContent, out List<AssetState> issues)
    {
        ParseCallCount++;
        if (ParseException != null)
            throw ParseException;
        var result = ParseImpl(csvContent);
        issues = result.issues;
        return result.items;
    }

    public string Serialize(List<CatalogItem> items) => string.Empty;
}

class FakeCliCsvCatalogLoader : CliCsvCatalogLoader
{
    public Func<string, CancellationToken, Task<(List<CatalogEntryDeploymentItem>, List<IDeploymentItem>)>>?
        ReadCatalogImpl;

    public int ReadCatalogCallCount;
    public List<string> WriteCatalogPaths = new();
    public Dictionary<string, List<CatalogItem>> WrittenItems = new();
    public List<string> DeletedPaths = new();
    public Exception? WriteException = null;
    public Exception? DeleteException = null;

    public FakeCliCsvCatalogLoader()
        : base(new FakeFileSystem(), new FakeCatalogCsvParser())
    {
    }

    public override async Task<(List<CatalogEntryDeploymentItem> entries, List<IDeploymentItem> failed)> ReadCatalog(
        string path, CancellationToken token)
    {
        ReadCatalogCallCount++;
        if (ReadCatalogImpl != null)
            return await ReadCatalogImpl(path, token);
        return (new List<CatalogEntryDeploymentItem>(), new List<IDeploymentItem>());
    }

    public override Task WriteCatalog(string path, List<CatalogItem> items, CancellationToken token)
    {
        if (WriteException != null)
        {
            throw WriteException;
        }

        WriteCatalogPaths.Add(path);
        WrittenItems[path] = items;
        return Task.CompletedTask;
    }

    public override Task DeleteCatalog(string path, CancellationToken token)
    {
        if (DeleteException != null)
        {
            throw DeleteException;
        }

        DeletedPaths.Add(path);
        return Task.CompletedTask;
    }
}

class FakeLogger : CoreLogger.ILogger
{
    public void LogError(object message)
    {
    }

    public void LogWarning(object message)
    {
    }

    public void LogInfo(object message)
    {
    }

    public void LogVerbose(object message)
    {
    }
}
