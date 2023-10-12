using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.CloudCode.Authoring.Fetch;

class JavaScriptFetchService : IFetchService
{
    readonly IUnityEnvironment m_UnityEnvironment;

    readonly IJavaScriptClient m_Client;

    readonly ICloudCodeScriptsLoader m_ScriptsLoader;

    readonly ICloudCodeInputParser m_InputParser;

    readonly ICloudCodeScriptParser m_ScriptParser;

    readonly IJavaScriptFetchHandler m_FetchHandler;

    public JavaScriptFetchService(
        IUnityEnvironment unityEnvironment,
        IJavaScriptClient client,
        ICloudCodeScriptsLoader scriptsLoader,
        ICloudCodeInputParser inputParser,
        ICloudCodeScriptParser scriptParser,
        IJavaScriptFetchHandler fetchHandler)
    {
        m_UnityEnvironment = unityEnvironment;
        m_Client = client;
        m_ScriptsLoader = scriptsLoader;
        m_InputParser = inputParser;
        m_ScriptParser = scriptParser;
        m_FetchHandler = fetchHandler;
    }

    public string ServiceType => CloudCodeConstants.ServiceTypeScripts;
    public string ServiceName => CloudCodeConstants.ServiceNameScripts;

    public IReadOnlyList<string> FileExtensions => new[]
    {
        CloudCodeConstants.FileExtensionJavaScript
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

        loadingContext?.Status($"Reading {ServiceType} files...");
        var loadResult = await GetResourcesFromFilesAsync(filePaths, cancellationToken);

        loadingContext?.Status($"Fetching {ServiceType} Files...");
        var result = await m_FetchHandler.FetchAsync(
            input.Path,
            loadResult.LoadedScripts,
            input.DryRun,
            input.Reconcile,
            cancellationToken);

        result = new FetchResult(
            created: result.Created,
            updated: result.Updated,
            deleted: result.Deleted,
            authored: result.Fetched,
            failed: result.Failed.Concat(loadResult.FailedContents.Cast<IDeploymentItem>()).ToList(),
            dryRun: input.DryRun);

        return result;
    }

    internal async Task<CloudCodeScriptLoadResult> GetResourcesFromFilesAsync(
        IReadOnlyList<string> filePaths, CancellationToken cancellationToken)
    {
        var loadResult = await m_ScriptsLoader
            .LoadScriptsAsync(
                filePaths,
                ServiceType,
                CloudCodeConstants.FileExtensionJavaScript,
                m_InputParser,
                m_ScriptParser,
                cancellationToken);
        return loadResult;
    }
}
