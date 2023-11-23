using System.Collections.ObjectModel;
using System.ComponentModel;
using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.CloudCode.Authoring;
using Unity.Services.Cli.CloudCode.Model;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment.ModuleGeneration;
using Unity.Services.CloudCode.Authoring.Editor.Core.Dotnet;
using Unity.Services.CloudCode.Authoring.Editor.Core.IO;
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

        var (loadedModules, failedModules) =
            await CloudCodeModulesLoader.LoadModulesAsync(ccmFilePaths, slnFilePaths, cancellationToken);

        loadingContext?.Status($"Deploying {m_ServiceType}...");

        var dryrun = deployInput.DryRun;
        var reconcile = deployInput.Reconcile;
        DeployResult result = null!;

        try
        {
            result = await CloudCodeDeploymentHandler.DeployAsync(loadedModules, reconcile, dryrun);
            failedModules.AddRange(result.Failed);
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

        return ConstructResult(loadedModules, result, deployInput, failedModules);
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

    static IDeploymentItem SetPathAsSolutionWhenAvailable(IScript item)
    {
        return new CloudCodeModule(
            item.Name.ToString(),
            string.IsNullOrEmpty(((CloudCodeModule)item).SolutionPath) ? ((CloudCodeModule)item).Path : ((CloudCodeModule)item).SolutionPath,
            ((CloudCodeModule)item).Progress,
            ((CloudCodeModule)item).Status);
    }

    static DeploymentResult ConstructResult(List<IScript> loadResult, DeployResult? result, DeployInput deployInput, List<IScript> failedModules)
    {
        DeploymentResult deployResult;
        if (result == null)
        {
            deployResult = new DeploymentResult(loadResult.OfType<IDeploymentItem>().ToList());
        }
        else
        {
            deployResult = new DeploymentResult(
                result.Updated.Select(SetPathAsSolutionWhenAvailable).ToList() as IReadOnlyList<IDeploymentItem>,
                ToDeleteDeploymentItems(result.Deleted, deployInput.DryRun),
                result.Created.Select(SetPathAsSolutionWhenAvailable).ToList() as IReadOnlyList<IDeploymentItem>,
                result.Deployed.Select(SetPathAsSolutionWhenAvailable).ToList() as IReadOnlyList<IDeploymentItem>,
                failedModules.Select(SetPathAsSolutionWhenAvailable).ToList() as IReadOnlyList<IDeploymentItem>,
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
