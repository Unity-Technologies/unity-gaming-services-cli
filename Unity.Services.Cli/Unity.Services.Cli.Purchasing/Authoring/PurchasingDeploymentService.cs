using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Authoring.Utils;
using Unity.Services.Cli.Purchasing.IO;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Deploy;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;
using Statuses = UnityEditor.Purchasing.Editor.Authoring.Core.Model.Statuses;

namespace Unity.Services.Cli.Purchasing.Authoring;

class PurchasingDeploymentService : PurchasingBaseService, IDeploymentService
{
    static readonly string[] k_FileExtensions = { Constants.FileExtension, Constants.CsvFileExtension };
    public override IReadOnlyList<string> FileExtensions => k_FileExtensions;

    readonly ICatalogDeploymentHandler m_DeploymentHandler;
    readonly CliCsvCatalogLoader m_CsvCatalogLoader;

    public PurchasingDeploymentService(
        ICatalogDeploymentHandler deploymentHandler,
        ILiveContentConfigClient client,
        ICatalogLoader catalogLoader,
        CliCsvCatalogLoader csvCatalogLoader)
        : base(client, catalogLoader)
    {
        m_DeploymentHandler = deploymentHandler;
        m_CsvCatalogLoader = csvCatalogLoader;
    }

    public async Task<DeploymentResult> Deploy(
        DeployInput deployInput,
        IReadOnlyList<AuthoringFile> authoringFiles,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        await m_Client.Initialize(environmentId, projectId, cancellationToken);

        loadingContext?.Status("Reading Purchasing files...");
        var filePaths = authoringFiles.ToPaths();
        var (entryItems, failedToLoad) =
            await LoadUcatFiles(filePaths, cancellationToken);
        var (csvEntries, csvFailed) =
            await LoadCsvFiles(filePaths, cancellationToken);
        entryItems.AddRange(csvEntries);
        failedToLoad.AddRange(csvFailed);

        loadingContext?.Status("Deploying Purchasing files...");
        var allEntries = entryItems.ToList();

        try
        {
            var res = await m_DeploymentHandler.DeployAsync(
                allEntries,
                dryRun: deployInput.DryRun,
                reconcile: deployInput.Reconcile,
                token: cancellationToken);

            var allDeployedItems = new List<IDeploymentItem>();
            allDeployedItems.AddRange(res.Deployed);
            allDeployedItems.AddRange(failedToLoad);

            return new PurchasingDeploymentResult(allDeployedItems, deployInput.DryRun);
        }
        catch (ClientException e)
        {
            foreach (var entry in entryItems)
                entry.Status = Statuses.GetFailedToDeploy(e.Message);

            var allItems = new List<IDeploymentItem>();
            allItems.AddRange(entryItems);
            allItems.AddRange(failedToLoad);

            return new PurchasingDeploymentResult(allItems, deployInput.DryRun);
        }
    }

    async Task<(List<CatalogEntryDeploymentItem> entries, List<IDeploymentItem> failed)> LoadCsvFiles(
        IReadOnlyList<string> filePaths,
        CancellationToken cancellationToken)
    {
        var entries = new List<CatalogEntryDeploymentItem>();
        var failed = new List<IDeploymentItem>();

        var csvFiles = filePaths
            .Where(p => p.EndsWith(Constants.CsvFileExtension, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var csvTasks = csvFiles.Select(f => m_CsvCatalogLoader.ReadCatalog(f, cancellationToken));
        var results = await Task.WhenAll(csvTasks);

        foreach (var (csvEntries, csvFailed) in results)
        {
            entries.AddRange(csvEntries);
            failed.AddRange(csvFailed);
        }

        return (entries, failed);
    }

    class PurchasingDeploymentResult : DeploymentResult
    {
        public PurchasingDeploymentResult(IReadOnlyList<IDeploymentItem> authored, bool dryRun)
            : base(
                GetItemsOfType(authored, Constants.Updated),
                GetItemsOfType(authored, Constants.Deleted),
                GetItemsOfType(authored, Constants.Created),
                authored.Where(a => a.Status.MessageSeverity == SeverityLevel.Success).ToList(),
                authored.Where(a => a.Status.MessageSeverity == SeverityLevel.Error).ToList(),
                dryRun)
        {
        }
    }
}
