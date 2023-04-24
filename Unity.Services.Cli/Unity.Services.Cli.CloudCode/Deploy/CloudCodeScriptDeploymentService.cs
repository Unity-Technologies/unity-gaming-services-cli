using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.CloudCode.Authoring;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeScriptDeploymentService : IDeploymentService
{
    internal ICloudCodeInputParser CloudCodeInputParser { get; }
    internal ICloudCodeScriptParser CloudCodeScriptParser { get; }
    internal ICliDeploymentOutputHandler CliDeploymentOutputHandler { get; }
    internal ICloudCodeScriptsLoader CloudCodeScriptsLoader { get; }
    internal ICliEnvironmentProvider EnvironmentProvider { get; }
    internal IJavaScriptClient CliCloudCodeClient { get; }
    internal ICloudCodeDeploymentHandler CloudCodeDeploymentHandler { get; }

    readonly string m_ServiceType;

    public CloudCodeScriptDeploymentService(
        ICloudCodeInputParser cloudCodeInputParser,
        ICloudCodeScriptParser cloudCodeScriptParser,
        IDeploymentHandlerWithOutput deployHandlerWithOutput,
        ICloudCodeScriptsLoader cloudCodeScriptsLoader,
        ICliEnvironmentProvider cliEnvironmentProvider,
        IJavaScriptClient cliCloudCodeClient)
    {
        CloudCodeInputParser = cloudCodeInputParser;
        CloudCodeScriptParser = cloudCodeScriptParser;
        CliDeploymentOutputHandler = deployHandlerWithOutput;
        CloudCodeScriptsLoader = cloudCodeScriptsLoader;
        EnvironmentProvider = cliEnvironmentProvider;
        CliCloudCodeClient = cliCloudCodeClient;
        CloudCodeDeploymentHandler = deployHandlerWithOutput;

        m_ServiceType = Constants.ServiceType;
        DeployFileExtension = Constants.JavaScriptFileExtension;
    }

    string IDeploymentService.ServiceType => m_ServiceType;
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
            CliDeploymentOutputHandler.Contents,
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
            return new DeploymentResult(CliDeploymentOutputHandler.Contents.ToList());
        }

        return new DeploymentResult(
            ToDeployContents(result.Created),
            ToDeployContents(result.Updated),
            ToDeleteContents(result.Deleted),
            ToDeployContents(result.Deployed),
            ToDeployContents(result.Failed).Concat(loadResult.FailedContents).ToList(),
            deployInput.DryRun
        );
    }

    IReadOnlyCollection<DeployContent> ToDeployContents(IReadOnlyList<IScript> scripts)
    {
        var contents = new List<DeployContent>();

        foreach (var script in scripts)
        {
            contents.AddRange(CliDeploymentOutputHandler.Contents.Where(deployContent => script.Path == deployContent.Path));
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
