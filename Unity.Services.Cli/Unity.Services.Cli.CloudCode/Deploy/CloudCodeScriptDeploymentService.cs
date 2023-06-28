using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.CloudCode.Authoring;
using Unity.Services.Cli.CloudCode.Model;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeScriptDeploymentService : IDeploymentService
{
    internal ICloudCodeInputParser CloudCodeInputParser { get; }
    internal ICloudCodeScriptParser CloudCodeScriptParser { get; }
    internal ICloudCodeScriptsLoader CloudCodeScriptsLoader { get; }
    internal ICliEnvironmentProvider EnvironmentProvider { get; }
    internal IJavaScriptClient CliCloudCodeClient { get; }
    internal ICloudCodeDeploymentHandler CloudCodeDeploymentHandler { get; }

    readonly string m_ServiceType;
    readonly string m_ServiceName;

    public CloudCodeScriptDeploymentService(
        ICloudCodeInputParser cloudCodeInputParser,
        ICloudCodeScriptParser cloudCodeScriptParser,
        ICloudCodeDeploymentHandler deployHandlerWithOutput,
        ICloudCodeScriptsLoader cloudCodeScriptsLoader,
        ICliEnvironmentProvider cliEnvironmentProvider,
        IJavaScriptClient cliCloudCodeClient)
    {
        CloudCodeInputParser = cloudCodeInputParser;
        CloudCodeScriptParser = cloudCodeScriptParser;
        CloudCodeScriptsLoader = cloudCodeScriptsLoader;
        EnvironmentProvider = cliEnvironmentProvider;
        CliCloudCodeClient = cliCloudCodeClient;
        CloudCodeDeploymentHandler = deployHandlerWithOutput;

        m_ServiceType = CloudCodeConstants.ServiceType;
        m_ServiceName = CloudCodeConstants.ServiceName;
        DeployFileExtension = CloudCodeConstants.JavaScriptFileExtension;
    }

    string IDeploymentService.ServiceType => m_ServiceType;
    string IDeploymentService.ServiceName => m_ServiceName;
    public string DeployFileExtension { get; }

    public async Task<DeploymentResult> Deploy(
        DeployInput deployInput,
        IReadOnlyList<string> filePaths,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        CliCloudCodeClient.Initialize(environmentId, projectId, cancellationToken);
        EnvironmentProvider.Current = environmentId;

        loadingContext?.Status($"Reading {m_ServiceType} Scripts...");

        var loadResult = await CloudCodeScriptsLoader.LoadScriptsAsync(
            filePaths,
            m_ServiceType,
            DeployFileExtension,
            CloudCodeInputParser,
            CloudCodeScriptParser,
            cancellationToken);

        loadingContext?.Status($"Deploying {m_ServiceType} Scripts...");

        DeployResult result = null!;
        try
        {
            result = await CloudCodeDeploymentHandler.DeployAsync(loadResult.LoadedScripts, deployInput.Reconcile, deployInput.DryRun);
        }
        catch (ApiException)
        {
            /*
             * Ignoring this because we already catch exceptions from UpdateScriptStatus() for each script and we don't
             * want to stop execution when a script generates an exception.
             */
        }
        catch (DeploymentException ex)
        {
            result = ex.Result;
        }

        if (result == null)
        {
            var deployContent = new List<IDeploymentItem>();

            deployContent.AddRange(loadResult.LoadedScripts.OfType<IDeploymentItem>().ToList());
            deployContent.AddRange(loadResult.FailedContents.OfType<IDeploymentItem>().ToList());

            return new DeploymentResult(deployContent);
        }

        var failedScripts = result.Failed.Select(item => item as IDeploymentItem)
                .Concat(loadResult.FailedContents.Select(item => item as IDeploymentItem))
                .ToList() as IReadOnlyList<IDeploymentItem>;

        return new DeploymentResult(
            result.Updated.Select(item => item as IDeploymentItem).ToList() as IReadOnlyList<IDeploymentItem>,
            ToDeleteContents(result.Deleted, deployInput.DryRun),
            result.Created.Select(item => item as IDeploymentItem).ToList() as IReadOnlyList<IDeploymentItem>,
            result.Deployed.Select(item => item as IDeploymentItem).ToList() as IReadOnlyList<IDeploymentItem>,
            failedScripts,
            deployInput.DryRun);
    }

    IReadOnlyList<IDeploymentItem> ToDeleteContents(IReadOnlyList<IScript> scripts, bool dryRun)
    {
        var contents = new List<IDeploymentItem>();

        foreach (var script in scripts)
        {
            var deletedCloudCode = new DeletedCloudCode(script.Name.ToString(), m_ServiceType, script.Path);
            contents.Add(deletedCloudCode);
            if (!dryRun)
            {
                deletedCloudCode.Status = new DeploymentStatus("Deployed", "Deleted remotely", SeverityLevel.Success);
                deletedCloudCode.Progress = 100f;
            }
        }

        return contents;
    }
}
