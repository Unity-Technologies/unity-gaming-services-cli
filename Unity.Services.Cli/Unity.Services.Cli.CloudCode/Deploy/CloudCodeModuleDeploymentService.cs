using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.CloudCode.Authoring;
using Unity.Services.Cli.CloudCode.Model;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeModuleDeploymentService : IDeploymentService
{
    internal ICloudCodeModulesLoader CloudCodeModulesLoader { get; }
    internal ICliEnvironmentProvider EnvironmentProvider { get; }
    internal ICSharpClient CliCloudCodeClient { get; }
    ICloudCodeDeploymentHandler CloudCodeDeploymentHandler { get; }

    public string ServiceType => m_ServiceType;

    public string ServiceName => m_ServiceName;

    public IReadOnlyList<string> FileExtensions { get; } = new[]
    {
        CloudCodeConstants.FileExtensionModulesCcm,
        CloudCodeConstants.FileExtensionModulesSln,
    };

    readonly string m_ServiceType;
    readonly string m_ServiceName;
    readonly IDeployFileService m_DeployFileService;

    public CloudCodeModuleDeploymentService(
        ICloudCodeDeploymentHandler deployHandler,
        ICloudCodeModulesLoader cloudCodeModulesLoader,
        ICliEnvironmentProvider environmentProvider,
        ICSharpClient client,
        IDeployFileService deployFileService)
    {
        CloudCodeModulesLoader = cloudCodeModulesLoader;
        EnvironmentProvider = environmentProvider;
        CliCloudCodeClient = client;
        CloudCodeDeploymentHandler = deployHandler;
        m_DeployFileService = deployFileService;

        m_ServiceType = CloudCodeConstants.ServiceTypeModules;
        m_ServiceName = CloudCodeConstants.ServiceNameModules;
    }

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

        loadingContext?.Status($"Reading {m_ServiceType}...");

        var (ccmFilePaths, slnFilePaths) = ListFilesToDeploy(filePaths.ToList());

        loadingContext?.Status($"Loading {m_ServiceName} modules...");

        var (loadedModules, failedToLoadModules) =
            await CloudCodeModulesLoader.LoadModulesAsync(ccmFilePaths, slnFilePaths, cancellationToken);

        loadingContext?.Status($"Deploying {m_ServiceType}...");

        var dryrun = deployInput.DryRun;
        var reconcile = deployInput.Reconcile;
        DeployResult result = null!;

        try
        {
            result = await CloudCodeDeploymentHandler.DeployAsync(loadedModules, reconcile, dryrun);
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

        return ConstructResult(loadedModules, result, deployInput, failedToLoadModules);
    }

    (List<string>, List<string>) ListFilesToDeploy(List<string> filePaths)
    {
        List<string> ccmFilePaths = new List<string>();
        List<string> slnFilePaths = new List<string>();
        if (filePaths.Count > 0)
        {
            ccmFilePaths = m_DeployFileService.ListFilesToDeploy(
                filePaths,
                CloudCodeConstants.FileExtensionModulesCcm,
                false).ToList();
            slnFilePaths = m_DeployFileService.ListFilesToDeploy(
                filePaths,
                CloudCodeConstants.FileExtensionModulesSln,
                false).ToList();
        }

        return (ccmFilePaths, slnFilePaths);
    }

    static void SetPathAsSolutionWhenAvailable(IReadOnlyList<IScript> items)
    {
        foreach (var item in items)
        {
            var ccm = (CloudCodeModule)item;
            if (!string.IsNullOrEmpty(ccm.SolutionPath))
                ccm.Path = ccm.SolutionPath;
        }
    }

    static DeploymentResult ConstructResult(List<IScript> loadResult, DeployResult? result, DeployInput deployInput, List<IScript> failedToLoad)
    {
        DeploymentResult deployResult;
        if (result == null)
        {
            deployResult = new DeploymentResult(loadResult.OfType<IDeploymentItem>().ToList());
        }
        else
        {
            var failed = result.Failed.Concat(failedToLoad).ToList();
            SetPathAsSolutionWhenAvailable(result.Failed);
            SetPathAsSolutionWhenAvailable(result.Created);
            SetPathAsSolutionWhenAvailable(result.Updated);
            SetPathAsSolutionWhenAvailable(failed);

            deployResult = new DeploymentResult(
                result.Updated.Cast<IDeploymentItem>().ToList(),
                ToDeleteDeploymentItems(result.Deleted, deployInput.DryRun),
                result.Created.Cast<IDeploymentItem>().ToList(),
                result.Deployed.Cast<IDeploymentItem>().ToList(),
                failed.Cast<IDeploymentItem>().ToList(),
                deployInput.DryRun);
        }

        return deployResult;
    }

    static IReadOnlyList<IDeploymentItem> ToDeleteDeploymentItems(IReadOnlyList<IScript> modules, bool dryRun)
    {
        var contents = new List<IDeploymentItem>();

        foreach (var module in modules)
        {
            var deletedCloudCode = new DeletedCloudCode(module.Name.ToString(), module.Language.ToString()!, module.Path);
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
