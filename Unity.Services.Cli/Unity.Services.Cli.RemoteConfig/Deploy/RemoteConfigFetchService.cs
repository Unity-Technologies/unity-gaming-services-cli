using Spectre.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Deploy.Input;
using Unity.Services.Cli.Deploy.Model;
using Unity.Services.Cli.Deploy.Service;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Fetch;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

class RemoteConfigFetchService : IFetchService
{
    private readonly IUnityEnvironment m_UnityEnvironment;
    private readonly IRemoteConfigFetchHandler m_FetchHandler;
    private readonly ICliRemoteConfigClient m_RemoteConfigClient;
    private readonly IDeployFileService m_DeployFileService;
    private readonly IRemoteConfigScriptsLoader m_RemoteConfigScriptsLoader;
    private readonly string m_DeployFileExtension;

    internal readonly string m_KeyFileMessageFormat = "Key '{0}' in file '{1}'";
    public string ServiceType { get; }

    string IFetchService.FileExtension => m_DeployFileExtension;

    public RemoteConfigFetchService(
        IUnityEnvironment unityEnvironment,
        IRemoteConfigFetchHandler fetchHandler,
        ICliRemoteConfigClient remoteConfigClient,
        IDeployFileService deployFileService,
        IRemoteConfigScriptsLoader remoteConfigScriptsLoader
    )
    {
        m_UnityEnvironment = unityEnvironment;
        m_FetchHandler = fetchHandler;
        m_RemoteConfigClient = remoteConfigClient;
        m_DeployFileService = deployFileService;
        m_RemoteConfigScriptsLoader = remoteConfigScriptsLoader;
        ServiceType = "Remote Config";
        m_DeployFileExtension = ".rc";
    }

    public async Task<FetchResult> FetchAsync(
        FetchInput input,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        var environmentId = await m_UnityEnvironment.FetchIdentifierAsync();
        m_RemoteConfigClient.Initialize(input.CloudProjectId!, environmentId, cancellationToken);
        var remoteConfigFiles = m_DeployFileService.ListFilesToDeploy(new[] {input.Path}, m_DeployFileExtension).ToList();

        var contents = new List<DeployContent>();
        var configFiles = await m_RemoteConfigScriptsLoader
            .LoadScriptsAsync(remoteConfigFiles, contents);
        loadingContext?.Status($"Fetching {ServiceType} Files...");

        Result fetchResult = await m_FetchHandler.FetchAsync(
                input.Path,
                configFiles,
                input.DryRun,
                input.Reconcile,
                cancellationToken);



        return new FetchResult(
            fetchResult.Updated.Select(kvp => string.Format(m_KeyFileMessageFormat, kvp.Key, NormalizePath(kvp.File)) ).ToList(),
            fetchResult.Deleted.Select(kvp => string.Format(m_KeyFileMessageFormat, kvp.Key, NormalizePath(kvp.File)) ).ToList(),
            fetchResult.Created.Select(kvp => string.Format(m_KeyFileMessageFormat, kvp.Key, NormalizePath(kvp.File)) ).ToList(),
            fetchResult.Fetched.Select(f => Path.GetRelativePath(".", f.Path) ).ToList(),
            fetchResult.Failed.Select(f => Path.GetRelativePath(".", f.Path)).ToList());
    }

    static string NormalizePath(string path)
    {
        return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }
}
