using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodePrecompiledModuleDeploymentService : IDeploymentService
{
    readonly ICliDeploymentOutputHandler m_CliDeploymentOutputHandler;
    readonly ICloudCodeModulesLoader m_CloudCodeModulesLoader;
    readonly ICliEnvironmentProvider m_EnvironmentProvider;
    readonly ICliCloudCodeClient m_CliCloudCodeClient;
    readonly ICloudCodeDeploymentHandler m_CloudCodeDeploymentHandler;

    readonly string m_ServiceType;
    readonly string m_DeployPrecompiledFileExtension;

    public CloudCodePrecompiledModuleDeploymentService(
        ICloudCodeServicesWrapper servicesWrapper
    )
    {
        m_CliDeploymentOutputHandler = servicesWrapper.CliDeploymentOutputHandler;
        m_CloudCodeModulesLoader = servicesWrapper.CloudCodeModulesLoader;
        m_EnvironmentProvider = servicesWrapper.EnvironmentProvider;
        m_CliCloudCodeClient = servicesWrapper.CliCloudCodeClient;
        m_CloudCodeDeploymentHandler = servicesWrapper.CloudCodeDeploymentHandler;
        m_ServiceType = "Cloud Code";
        m_DeployPrecompiledFileExtension = ".ccm";
    }

    string IDeploymentService.ServiceType => m_ServiceType;

    string IDeploymentService.DeployFileExtension => m_DeployPrecompiledFileExtension;

    public async Task<DeploymentResult> Deploy(
        DeployInput deployInput,
        IReadOnlyList<string> filePaths,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        m_CliCloudCodeClient.Initialize(environmentId, projectId, cancellationToken);
        m_EnvironmentProvider.Current = environmentId;

        loadingContext?.Status($"Reading {m_ServiceType} Modules ...");

        var modules = await m_CloudCodeModulesLoader.LoadPrecompiledModulesAsync(
            filePaths,
            m_ServiceType,
            m_DeployPrecompiledFileExtension,
            m_CliDeploymentOutputHandler.Contents);


        loadingContext?.Status($"Deploying {m_ServiceType} Modules...");

        var dryrun = deployInput.DryRun;
        var reconcile = deployInput.Reconcile;
        DeployResult result = null!;

        try
        {
            result = await m_CloudCodeDeploymentHandler.DeployAsync(modules, reconcile, dryrun);
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

        if (result == null || modules == null)
        {
            return new DeploymentResult(m_CliDeploymentOutputHandler.Contents.ToList());
        }

        return new DeploymentResult(
            ToDeployContents(result.Created),
            ToDeployContents(result.Updated),
            ToDeleteContents(result.Deleted),
            ToDeployContents(result.Deployed),
            ToDeployContents(result.Failed),
            dryrun
        );
    }

    IReadOnlyCollection<DeployContent> ToDeployContents(IReadOnlyList<IScript> modules)
    {
        var contents = new List<DeployContent>();

        foreach (var module in modules)
        {
            contents.AddRange(m_CliDeploymentOutputHandler.Contents.Where(deployContent => module.Path == deployContent.Path));
        }

        return contents;
    }

    static IReadOnlyCollection<DeployContent> ToDeleteContents(IReadOnlyList<IScript> modules)
    {
        var contents = new List<DeployContent>();

        foreach (var module in modules)
        {
            contents.Add(new DeployContent(module.Name.ToString(), module.Language.ToString()!, module.Path));
        }

        return contents;
    }
}
