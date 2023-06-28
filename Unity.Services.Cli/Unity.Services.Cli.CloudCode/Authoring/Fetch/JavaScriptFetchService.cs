using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudCode.Authoring;

class JavaScriptFetchService : IFetchService
{
    readonly IUnityEnvironment m_UnityEnvironment;

    readonly IJavaScriptClient m_Client;

    readonly IDeployFileService m_DeployFileService;

    readonly ICloudCodeScriptsLoader m_ScriptsLoader;

    readonly ICloudCodeInputParser m_InputParser;

    readonly ICloudCodeScriptParser m_ScriptParser;

    readonly IJavaScriptFetchHandler m_FetchHandler;

    public JavaScriptFetchService(
        IUnityEnvironment unityEnvironment,
        IJavaScriptClient client,
        IDeployFileService deployFileService,
        ICloudCodeScriptsLoader scriptsLoader,
        ICloudCodeInputParser inputParser,
        ICloudCodeScriptParser scriptParser,
        IJavaScriptFetchHandler fetchHandler)
    {
        m_UnityEnvironment = unityEnvironment;
        m_Client = client;
        m_DeployFileService = deployFileService;
        m_ScriptsLoader = scriptsLoader;
        m_InputParser = inputParser;
        m_ScriptParser = scriptParser;
        m_FetchHandler = fetchHandler;
    }

    public string ServiceType => CloudCodeConstants.ServiceType;
    public string ServiceName => CloudCodeConstants.ServiceName;

    string IFetchService.FileExtension => CloudCodeConstants.JavaScriptFileExtension;

    public async Task<FetchResult> FetchAsync(
        FetchInput input, StatusContext? loadingContext, CancellationToken cancellationToken)
    {
        var environmentId = await m_UnityEnvironment.FetchIdentifierAsync();
        m_Client.Initialize(environmentId, input.CloudProjectId!, cancellationToken);

        loadingContext?.Status($"Reading {ServiceType} files...");
        var loadResult = await GetResourcesFromFilesAsync(input, cancellationToken);

        loadingContext?.Status($"Fetching {ServiceType} Files...");
        var result = await m_FetchHandler.FetchAsync(
            input.Path,
            loadResult.LoadedScripts,
            input.DryRun,
            input.Reconcile,
            cancellationToken);

        return result;
    }

    internal async Task<CloudCodeScriptLoadResult> GetResourcesFromFilesAsync(
        FetchInput input, CancellationToken cancellationToken)
    {
        var files = m_DeployFileService.ListFilesToDeploy(
            new[]
            {
                input.Path
            },
            CloudCodeConstants.JavaScriptFileExtension);

        var loadResult = await m_ScriptsLoader
            .LoadScriptsAsync(
                files,
                ServiceType,
                CloudCodeConstants.JavaScriptFileExtension,
                m_InputParser,
                m_ScriptParser,
                cancellationToken);
        return loadResult;
    }
}
