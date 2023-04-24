using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.CloudCode.Authoring;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodePrecompiledModuleDeploymentService : IDeploymentService
{
    internal ICliDeploymentOutputHandler CliDeploymentOutputHandler { get; }
    internal ICloudCodeModulesLoader CloudCodeModulesLoader { get; }
    internal ICliEnvironmentProvider EnvironmentProvider { get; }
    internal ICSharpClient CliCloudCodeClient { get; }
    internal ICloudCodeDeploymentHandler CloudCodeDeploymentHandler { get; }

    readonly string m_ServiceType;
    readonly string m_DeployPrecompiledFileExtension;

    public CloudCodePrecompiledModuleDeploymentService(
        IDeploymentHandlerWithOutput deployHandlerWithOutput,
        ICloudCodeModulesLoader cloudCodeModulesLoader,
        ICliEnvironmentProvider environmentProvider,
        ICSharpClient client)
    {
        CliDeploymentOutputHandler = deployHandlerWithOutput;
        CloudCodeModulesLoader = cloudCodeModulesLoader;
        EnvironmentProvider = environmentProvider;
        CliCloudCodeClient = client;
        CloudCodeDeploymentHandler = deployHandlerWithOutput;
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
        CliCloudCodeClient.Initialize(environmentId, projectId, cancellationToken);
        EnvironmentProvider.Current = environmentId;

        loadingContext?.Status($"Reading {m_ServiceType} Modules ...");

        var modules = await CloudCodeModulesLoader.LoadPrecompiledModulesAsync(
            filePaths,
            m_ServiceType,
            m_DeployPrecompiledFileExtension,
            CliDeploymentOutputHandler.Contents);

        loadingContext?.Status($"Deploying {m_ServiceType} Modules...");

        var dryrun = deployInput.DryRun;
        var reconcile = deployInput.Reconcile;
        DeployResult result = null!;

        try
        {
            result = await CloudCodeDeploymentHandler.DeployAsync(modules, reconcile, dryrun);
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
            return new DeploymentResult(CliDeploymentOutputHandler.Contents.ToList());
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
            contents.AddRange(CliDeploymentOutputHandler.Contents.Where(deployContent => module.Path == deployContent.Path));
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
