using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.CloudContentDelivery.Authoring.Core.Fetch;
using Unity.Services.CloudContentDelivery.Authoring.Core.Model;
using Unity.Services.CloudContentDelivery.Authoring.Core.Service;
using FetchResult = Unity.Services.Cli.Authoring.Model.FetchResult;

namespace Unity.Services.Cli.CloudContentDelivery.Fetch;

class CloudContentDeliveryFetchService : IFetchService
{
    readonly ICloudContentDeliveryFetchHandler m_FetchHandler;
    readonly ICloudContentDeliveryClient m_Client;
    const string k_DeployFileExtension = ".ccd";
    const string k_ServiceType = "CloudContentDelivery";

    public CloudContentDeliveryFetchService(
        ICloudContentDeliveryFetchHandler fetchHandler,
        ICloudContentDeliveryClient client)
    {
        m_FetchHandler = fetchHandler;
        m_Client = client;
    }

    public string ServiceType => k_ServiceType;
    public string ServiceName { get; } = k_ServiceType;

    public IReadOnlyList<string> FileExtensions => new[]
    {
        k_DeployFileExtension
    };

    public async Task<FetchResult> FetchAsync(
        FetchInput input,
        IReadOnlyList<string> filePaths,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        m_Client.Initialize(environmentId, projectId, cancellationToken);
        loadingContext?.Status($"Reading {k_ServiceType} files...");
        var resources = await GetResourcesFromFiles(filePaths);

        loadingContext?.Status($"Fetching {k_ServiceType} files...");
        var res = await m_FetchHandler.FetchAsync(
            input.Path,
            resources,
            input.DryRun,
            input.Reconcile,
            cancellationToken);

        return new FetchResult(
            res.Updated,
            res.Deleted,
            res.Created,
            res.Fetched,
            res.Failed,
            input.DryRun
        );
    }

    static async Task<IReadOnlyList<IBucket>> GetResourcesFromFiles(IReadOnlyList<string> filePaths)
    {
        var resources = await Task.WhenAll(filePaths.Select(GetResourcesFromFile));

        return resources.SelectMany(r => r).ToList();
    }

    static Task<List<IBucket>> GetResourcesFromFile(string filePath)
    {
        // A file may contain more than one resource if the resources are small
        throw new NotImplementedException();
    }


}
