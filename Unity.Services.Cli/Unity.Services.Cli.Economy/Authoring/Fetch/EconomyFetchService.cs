using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Economy.Editor.Authoring.Core.Fetch;
using Unity.Services.Economy.Editor.Authoring.Core.Model;
using FetchResult = Unity.Services.Cli.Authoring.Model.FetchResult;

namespace Unity.Services.Cli.Economy.Authoring.Fetch;

class EconomyFetchService : IFetchService
{
    readonly IUnityEnvironment m_UnityEnvironment;
    readonly ICliEconomyClient m_Client;
    readonly IEconomyResourcesLoader m_EconomyResourcesLoader;
    readonly IEconomyFetchHandler m_EconomyFetchHandler;

    public string ServiceType => Constants.ServiceType;
    public string ServiceName => Constants.ServiceName;
    public IReadOnlyList<string> FileExtensions => new[]
    {
        EconomyResourcesExtensions.Currency,
        EconomyResourcesExtensions.InventoryItem,
        EconomyResourcesExtensions.MoneyPurchase,
        EconomyResourcesExtensions.VirtualPurchase
    };

    public EconomyFetchService(
        IUnityEnvironment unityEnvironment,
        ICliEconomyClient economyClient,
        IEconomyResourcesLoader resourcesLoader,
        IEconomyFetchHandler economyFetchHandler)
    {
        m_UnityEnvironment = unityEnvironment;
        m_Client = economyClient;
        m_EconomyResourcesLoader = resourcesLoader;
        m_EconomyFetchHandler = economyFetchHandler;
    }

    public async Task<FetchResult> FetchAsync(
        FetchInput input,
        IReadOnlyList<string> filePaths,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        m_Client.Initialize(environmentId, projectId, cancellationToken);
        loadingContext?.Status($"Reading {ServiceType} files...");

        var tasks = filePaths
            .Select(path => m_EconomyResourcesLoader.LoadResourceAsync(path, cancellationToken))
            .ToList();

        await Task.WhenAll(tasks);

        var resources = tasks
            .Select(task => task.Result)
            .ToList();

        loadingContext?.Status($"Fetching {ServiceType} Files...");

        var economyFetchResult = await m_EconomyFetchHandler.FetchAsync(
            input.Path,
            resources,
            input.DryRun,
            input.Reconcile,
            cancellationToken);

        return new FetchResult(
            economyFetchResult.Updated,
            economyFetchResult.Deleted,
            economyFetchResult.Created,
            economyFetchResult.Fetched,
            economyFetchResult.Failed,
            input.DryRun);
    }
}
