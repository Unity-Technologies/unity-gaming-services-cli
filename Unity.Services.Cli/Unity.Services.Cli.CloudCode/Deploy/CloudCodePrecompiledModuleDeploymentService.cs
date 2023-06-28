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

class CloudCodePrecompiledModuleDeploymentService : IDeploymentService
{
    internal ICloudCodeModulesLoader CloudCodeModulesLoader { get; }
    internal ICliEnvironmentProvider EnvironmentProvider { get; }
    internal ICSharpClient CliCloudCodeClient { get; }
    ICloudCodeDeploymentHandler CloudCodeDeploymentHandler { get; }

    readonly string m_ServiceType;
    readonly string m_ServiceName;
    readonly string m_DeployPrecompiledFileExtension;

    public CloudCodePrecompiledModuleDeploymentService(
        ICloudCodeDeploymentHandler deployHandler,
        ICloudCodeModulesLoader cloudCodeModulesLoader,
        ICliEnvironmentProvider environmentProvider,
        ICSharpClient client)
    {
        CloudCodeModulesLoader = cloudCodeModulesLoader;
        EnvironmentProvider = environmentProvider;
        CliCloudCodeClient = client;
        CloudCodeDeploymentHandler = deployHandler;
        m_ServiceType = CloudCodeConstants.ServiceTypeModules;
        m_ServiceName = CloudCodeConstants.ServiceNameModule;
        m_DeployPrecompiledFileExtension = ".ccm";
    }

    string IDeploymentService.ServiceType => m_ServiceType;

    string IDeploymentService.ServiceName => m_ServiceName;

    string IDeploymentService.DeployFileExtension => m_DeployPrecompiledFileExtension;

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

        var loadResult = await CloudCodeModulesLoader.LoadPrecompiledModulesAsync(
            filePaths,
            m_ServiceType);


        loadingContext?.Status($"Deploying {m_ServiceType}...");

        var dryrun = deployInput.DryRun;
        var reconcile = deployInput.Reconcile;
        DeployResult result = null!;

        try
        {
            result = await CloudCodeDeploymentHandler.DeployAsync(loadResult, reconcile, dryrun);
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
            return new DeploymentResult(loadResult.OfType<IDeploymentItem>().ToList());
        }

        return new DeploymentResult(
            result.Updated.Select(item => item as IDeploymentItem).ToList() as IReadOnlyList<IDeploymentItem>,
            ToDeleteDeploymentItems(result.Deleted, deployInput.DryRun),
            result.Created.Select(item => item as IDeploymentItem).ToList() as IReadOnlyList<IDeploymentItem>,
            result.Deployed.Select(item => item as IDeploymentItem).ToList() as IReadOnlyList<IDeploymentItem>,
            result.Failed.Select(item => item as IDeploymentItem).ToList() as IReadOnlyList<IDeploymentItem>,
            dryrun);
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
