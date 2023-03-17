using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeScriptDeploymentService : IDeploymentService
{
    readonly ICloudCodeInputParser m_CloudCodeInputParser;
    readonly ICloudCodeService m_CloudCodeService;
    readonly ICliDeploymentOutputHandler m_CliDeploymentOutputHandler;
    readonly ICloudCodeScriptsLoader m_CloudCodeScriptsLoader;
    readonly ICliEnvironmentProvider m_EnvironmentProvider;
    readonly ICliCloudCodeClient m_CliCloudCodeClient;
    readonly ICloudCodeDeploymentHandler m_CloudCodeDeploymentHandler;
    string m_ServiceType;

    public CloudCodeScriptDeploymentService(
        ICloudCodeServicesWrapper servicesWrapper
    )
    {
        m_CloudCodeInputParser = servicesWrapper.CloudCodeInputParser;
        m_CloudCodeService = servicesWrapper.CloudCodeService;
        m_CliDeploymentOutputHandler = servicesWrapper.CliDeploymentOutputHandler;
        m_CloudCodeScriptsLoader = servicesWrapper.CloudCodeScriptsLoader;
        m_EnvironmentProvider = servicesWrapper.EnvironmentProvider;
        m_CliCloudCodeClient = servicesWrapper.CliCloudCodeClient;
        m_CloudCodeDeploymentHandler = servicesWrapper.CloudCodeDeploymentHandler;

        m_ServiceType = "Cloud Code";
        DeployFileExtension = ".js";
    }

    string IDeploymentService.ServiceType => m_ServiceType;
    public string DeployFileExtension { get; }

    public async Task<DeploymentResult> Deploy(
        DeployInput input,
        IReadOnlyList<string> filePaths,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        m_CliCloudCodeClient.Initialize(environmentId, projectId, cancellationToken);
        m_EnvironmentProvider.Current = environmentId;

        loadingContext?.Status($"Reading {m_ServiceType} Scripts...");

        var scriptList = await m_CloudCodeScriptsLoader.LoadScriptsAsync(
            filePaths,
            m_ServiceType,
            DeployFileExtension,
            m_CloudCodeInputParser,
            m_CloudCodeService,
            m_CliDeploymentOutputHandler.Contents,
            cancellationToken);

        loadingContext?.Status($"Deploying {m_ServiceType} Scripts...");

        var dryrun = input.DryRun;
        var reconcile = input.Reconcile;
        DeployResult result = null!;

        try
        {
            result = await m_CloudCodeDeploymentHandler.DeployAsync(scriptList, reconcile, dryrun);
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

        if (result == null || scriptList == null)
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

    IReadOnlyCollection<DeployContent> ToDeployContents(IReadOnlyList<IScript> scripts)
    {
        var contents = new List<DeployContent>();

        foreach (var script in scripts)
        {
            contents.AddRange(m_CliDeploymentOutputHandler.Contents.Where(deployContent => script.Path == deployContent.Path));
        }

        return contents;
    }

    static IReadOnlyCollection<DeployContent> ToDeleteContents(IReadOnlyList<IScript> scripts)
    {
        var contents = new List<DeployContent>();

        foreach (var script in scripts)
        {
            contents.Add(new DeployContent(script.Name.ToString(), script.Language.ToString()!, script.Path));
        }

        return contents;
    }
}
