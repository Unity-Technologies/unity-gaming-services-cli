using Spectre.Console;
using Unity.Services.Access.Authoring.Core.Deploy;
using Unity.Services.Access.Authoring.Core.Model;
using Unity.Services.Access.Authoring.Core.Service;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;

namespace Unity.Services.Cli.Access.Deploy;

public class ProjectAccessDeploymentService : IDeploymentService
{
    readonly string m_ServiceType;
    readonly string m_ServiceName;
    readonly string m_DeployFileExtension;
    readonly IProjectAccessClient m_ProjectAccessClient;
    readonly IProjectAccessDeploymentHandler m_DeploymentHandler;
    readonly IAccessConfigLoader m_AccessConfigConfigLoader;

    public ProjectAccessDeploymentService(
        IProjectAccessClient projectAccessClient,
        IProjectAccessDeploymentHandler projectAccessDeploymentHandler,
        IAccessConfigLoader projectAccessConfigLoader
    )
    {
        m_ProjectAccessClient = projectAccessClient;
        m_DeploymentHandler = projectAccessDeploymentHandler;
        m_AccessConfigConfigLoader = projectAccessConfigLoader;
        m_ServiceType = "Access";
        m_ServiceName = "access";
        m_DeployFileExtension = ".ac";
    }

    public string ServiceType => m_ServiceType;

    public string ServiceName => m_ServiceName;

    public IReadOnlyList<string> FileExtensions => new[]
    {
        m_DeployFileExtension
    };

    public async Task<DeploymentResult> Deploy(
        DeployInput deployInput,
        IReadOnlyList<string> filePaths,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        m_ProjectAccessClient.Initialize(environmentId, projectId, cancellationToken);
        var files = await m_AccessConfigConfigLoader.LoadFilesAsync(filePaths, cancellationToken);
        var loadedFiles = files.Loaded;
        var failedFiles = files.Failed;

        loadingContext?.Status($"Deploying {m_ServiceType} Files...");

        var deployStatusList = await m_DeploymentHandler.DeployAsync(
            loadedFiles,
            deployInput.DryRun,
            deployInput.Reconcile);

        return new AccessDeploymentResult(
            deployStatusList.Updated,
            deployStatusList.Deleted,
            deployStatusList.Created,
            deployStatusList.Deployed.Select(d => (ProjectAccessFile)d).ToList(),
            deployStatusList.Failed.Concat(failedFiles).Select(d => (ProjectAccessFile)d).ToList());
    }
}
