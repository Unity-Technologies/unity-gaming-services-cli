using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.Authoring.Handlers;

static class DeployCommandHandler
{
    public static async Task DeployAsync(
        IHost host,
        DeployInput input,
        IUnityEnvironment unityEnvironment,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        ICliDeploymentDefinitionService definitionService,
        IAnalyticsEventBuilder analyticsEventBuilder,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync(
            $"Deploying files...",
            context => DeployAsync(
                host,
                input,
                unityEnvironment,
                logger,
                context,
                definitionService,
                analyticsEventBuilder,
                cancellationToken));
    }

    internal static async Task DeployAsync(
        IHost host,
        DeployInput input,
        IUnityEnvironment unityEnvironment,
        ILogger logger,
        StatusContext? loadingContext,
        ICliDeploymentDefinitionService definitionService,
        IAnalyticsEventBuilder analyticsEventBuilder,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string> inputPaths = input.Paths;
        var services = host.Services.GetServices<IDeploymentService>().ToList();

        if (!AuthoringHandlerCommon.PreActionValidation(input, logger, services, inputPaths))
        {
            return;
        }

        var deploymentServices = services
            .Where(s => AuthoringHandlerCommon.CheckService(input, s.ServiceName))
            .ToArray();

        var ddefResult = AuthoringHandlerCommon.GetDdefResult(
            definitionService,
            logger,
            input.Paths,
            deploymentServices.SelectMany(ds => ds.FileExtensions));

        if (ddefResult == null)
        {
            return;
        }

        AuthoringHandlerCommon.SendAnalytics(analyticsEventBuilder, inputPaths, deploymentServices);

        var projectId = input.CloudProjectId!;
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

        var authoringResultServiceTask = deploymentServices
                .Select<IDeploymentService, AuthoringResultServiceTask<DeploymentResult>>(
                service =>
                {
                    var filePaths = service.FileExtensions
                        .SelectMany(extension => ddefResult.AllFilesByExtension[extension])
                        .ToArray();

                    if (!input.Reconcile && !filePaths.Any())
                    {
                        // nothing to do for this service
                        return new AuthoringResultServiceTask<DeploymentResult>(
                            Task.FromResult(new DeploymentResult(Array.Empty<AuthorResult>())),
                            service.ServiceType);
                    }

                    return new AuthoringResultServiceTask<DeploymentResult>(
                        service.Deploy(
                            input,
                            filePaths,
                            projectId,
                            environmentId,
                            loadingContext,
                            cancellationToken),
                        service.ServiceType);
                })
            .ToArray();

        try
        {
            await Task.WhenAll(
                authoringResultServiceTask
                    .Select(t => t.AuthorResultTask));
        }
        catch
        {
            // do nothing
            // this allows us to capture all the exceptions
            // and handle them below
        }

        // Get Results from successfully ran deployments
        var deploymentResults = authoringResultServiceTask
            .Select(t => t.AuthorResultTask)
            .Where(t => t.IsCompletedSuccessfully)
            .Select(t => t.Result)
            .ToArray();

        var totalResult = new DeploymentResult(deploymentResults, input.DryRun);

        AuthoringHandlerCommon.PrintResult(
            input,
            logger,
            authoringResultServiceTask,
            totalResult,
            ddefResult);
    }
}
