using Spectre.Console;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Fetch;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Model;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Service;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;

namespace Unity.Services.Cli.Access.Deploy;

class ProjectAccessFetchService : IFetchService
{
    readonly IProjectAccessFetchHandler m_FetchHandler;
    readonly IProjectAccessClient m_ProjectAccessClient;
    readonly IAccessConfigLoader m_AccessConfigConfigLoader;
    readonly string m_ServiceType;
    readonly string m_ServiceName;
    readonly string m_FileExtension;

    public ProjectAccessFetchService(
        IProjectAccessClient projectAccessClient,
        IProjectAccessFetchHandler projectAccessFetchHandler,
        IAccessConfigLoader projectAccessConfigLoader)
    {
        m_ProjectAccessClient = projectAccessClient;
        m_FetchHandler = projectAccessFetchHandler;
        m_AccessConfigConfigLoader = projectAccessConfigLoader;
        m_ServiceType = "Access";
        m_ServiceName = "access";
        m_FileExtension = ".ac";
    }

    public string ServiceType => m_ServiceType;
    public string ServiceName => m_ServiceName;
    public IReadOnlyList<string> FileExtensions => new[]
    {
        m_FileExtension
    };
    public async Task<FetchResult> FetchAsync(
        FetchInput input,
        IReadOnlyList<string> filePaths,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        m_ProjectAccessClient.Initialize(environmentId, projectId, cancellationToken);
        var loadResult = await m_AccessConfigConfigLoader.LoadFilesAsync(filePaths, cancellationToken);
        var configFiles = loadResult.Loaded.ToList();

        loadingContext?.Status($"Fetching {ServiceType} Files...");
        var fetchStatusList = await m_FetchHandler.FetchAsync(
            input.Path,
            configFiles,
            input.DryRun,
            input.Reconcile,
            cancellationToken);

        return new AccessFetchResult(
            updated: fetchStatusList.Updated,
            deleted: fetchStatusList.Deleted,
            created: fetchStatusList.Created,
            authored: fetchStatusList.Fetched.Select(d => (ProjectAccessFile)d).ToList(),
            failed: fetchStatusList.Failed.Select(d => (ProjectAccessFile)d).ToList());
    }
}
