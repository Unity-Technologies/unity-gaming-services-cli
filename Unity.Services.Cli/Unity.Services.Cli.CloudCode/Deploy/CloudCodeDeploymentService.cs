using Spectre.Console;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Deploy.Input;
using Unity.Services.Cli.Deploy.Model;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Deploy.Service;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;

namespace Unity.Services.Cli.CloudCode.Deploy;

internal class CloudCodeDeploymentService : IDeploymentService
{
    readonly IUnityEnvironment m_UnityEnvironment;
    readonly ICloudCodeInputParser m_CloudCodeInputParser;
    readonly ICloudCodeService m_CloudCodeService;
    readonly ICliDeploymentOutputHandler m_CliDeploymentOutputHandler;
    readonly ICloudCodeScriptsLoader m_CloudCodeScriptsLoader;
    readonly ICliEnvironmentProvider m_EnvironmentProvider;
    readonly ICliCloudCodeClient m_CliCloudCodeClient;
    readonly ICloudCodeDeploymentHandler m_CloudCodeDeploymentHandler;
    string m_ServiceType;
    string m_DeployFileExtension;

    public CloudCodeDeploymentService(
            IUnityEnvironment unityEnvironment,
            ICloudCodeServicesWrapper servicesWrapper
            )
    {
        m_UnityEnvironment = unityEnvironment;

        m_CloudCodeInputParser = servicesWrapper.CloudCodeInputParser;
        m_CloudCodeService = servicesWrapper.CloudCodeService;
        m_CliDeploymentOutputHandler = servicesWrapper.CliDeploymentOutputHandler;
        m_CloudCodeScriptsLoader = servicesWrapper.CloudCodeScriptsLoader;
        m_EnvironmentProvider = servicesWrapper.EnvironmentProvider;
        m_CliCloudCodeClient = servicesWrapper.CliCloudCodeClient;
        m_CloudCodeDeploymentHandler = servicesWrapper.CloudCodeDeploymentHandler;

        m_ServiceType = "Cloud Code";
        m_DeployFileExtension = ".js";
    }

    string IDeploymentService.ServiceType => m_ServiceType;

    string IDeploymentService.DeployFileExtension => m_DeployFileExtension;

    public async Task<DeploymentResult> Deploy(
        DeployInput input,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        var environmentId = await m_UnityEnvironment.FetchIdentifierAsync();

        m_CliCloudCodeClient.Initialize(environmentId, input.CloudProjectId!, cancellationToken);
        m_EnvironmentProvider.Current = environmentId;

        loadingContext?.Status($"Reading {m_ServiceType} Scripts...");

        var scriptList = await m_CloudCodeScriptsLoader.LoadScriptsAsync(
            input.Paths,
            m_ServiceType,
            m_DeployFileExtension,
            m_CloudCodeInputParser,
            m_CloudCodeService,
            m_CliDeploymentOutputHandler.Contents,
            cancellationToken);

        loadingContext?.Status($"Deploying {m_ServiceType} Scripts...");
        try
        {
            await m_CloudCodeDeploymentHandler.DeployAsync(scriptList);
        }
        catch (ApiException)
        {
            /*
             * Ignoring this because we already catch exceptions from UpdateScriptStatus() for each script and we don't
             * want to stop execution when a script generates an exception.
             */
        }

        return new DeploymentResult(m_CliDeploymentOutputHandler.Contents.ToList());
    }
}
