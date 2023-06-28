using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.Authoring.Handlers;

static class DeployHandler
{
    public static async Task DeployAsync(
        IHost host,
        DeployInput input,
        IDeployFileService deployFileService,
        IUnityEnvironment unityEnvironment,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync(
            $"Deploying files...",
            context => DeployAsync(
                host,
                input,
                deployFileService,
                unityEnvironment,
                logger,
                context,
                cancellationToken));
    }

    internal static async Task DeployAsync(
        IHost host,
        DeployInput input,
        IDeployFileService deployFileService,
        IUnityEnvironment unityEnvironment,
        ILogger logger,
        StatusContext? loadingContext,
        CancellationToken cancellationToken
    )
    {
        var services = host.Services.GetServices<IDeploymentService>().ToList();

        var supportedServicesStr = string.Join(", ", services.Select(s => s.ServiceType));
        var supportedServiceNamesStr = string.Join(", ", services.Select(s => s.ServiceName));

        bool areAllServicesSupported = !AreAllServicesSupported(input, services, out var unsupportedServicesStr);
        if (input.Paths.Count == 0 || areAllServicesSupported)
        {
            if (areAllServicesSupported)
            {
                logger.LogError($"These service options were not recognized: {unsupportedServicesStr}.");
            }

            logger.LogInformation($"Currently supported services are: {supportedServicesStr}." +
                $"{Environment.NewLine}    You can filter your service(s) with the --services option: " +
                $"{supportedServiceNamesStr}");
        }

        if (input.Reconcile && input.Services.Count == 0)
        {
            logger.LogError(
                "Reconcile is a destructive operation. Specify your service(s) with the --services option: {SupportedServiceNamesStr}",
                supportedServiceNamesStr);
            return;
        }

        var environmentId = await unityEnvironment.FetchIdentifierAsync();
        var projectId = input.CloudProjectId!;
        var deploymentServices = services
            .Where(s => CheckService(input, s))
            .ToArray();

        var tasks = deploymentServices.Select(
                m => m.Deploy(
                    input,
                    deployFileService.ListFilesToDeploy(input.Paths, m.DeployFileExtension),
                    projectId,
                    environmentId,
                    loadingContext,
                    cancellationToken))
            .ToArray();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            // do nothing
            // this allows us to capture all the exceptions
            // and handle them below
        }

        // Get Results from successfully ran deployments
        var deploymentResults = tasks
            .Where(t => t.IsCompletedSuccessfully)
            .Select(t => t.Result)
            .ToArray();

        var totalResult = new DeploymentResult(
            deploymentResults.SelectMany(x => x.Updated).ToList(),
            deploymentResults.SelectMany(x => x.Deleted).ToList(),
            deploymentResults.SelectMany(x => x.Created).ToList(),
            deploymentResults.SelectMany(x => x.Deployed).ToList(),
            deploymentResults.SelectMany(x => x.Failed).ToList(),
            input.DryRun
        );

        logger.LogResultValue(totalResult);

        // Get Exceptions from faulted deployments
        var exceptions = tasks
            .Where(t => t.IsFaulted)
            .Select(t => t.Exception?.InnerException)
            .ToList();

        if (exceptions.Any())
        {
            throw new AggregateException(exceptions!);
        }

        if (totalResult.Failed.Any())
        {
            throw new DeploymentFailureException();
        }
    }

    static bool CheckService(DeployInput input, IDeploymentService service)
    {
        if (!input.Reconcile && input.Services.Count == 0)
            return true;

        return input.Services.Contains(service.ServiceName);
    }

    static bool AreAllServicesSupported(DeployInput input, IReadOnlyList<IDeploymentService> services, out string unsupportedServices)
    {
        var serviceNames = services.Select(s => s.ServiceName);

        unsupportedServices = string.Join(", ", input.Services.Except(serviceNames));

        return string.IsNullOrEmpty(unsupportedServices);
    }
}
