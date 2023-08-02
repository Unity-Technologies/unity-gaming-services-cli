using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Model.TableOutput;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent;

namespace Unity.Services.Cli.Authoring.Handlers;

static class FetchHandler
{
    public static async Task FetchAsync(
        IHost host,
        FetchInput input,
        ILogger logger,
        ICliDeploymentDefinitionService deploymentDefinitionService,
        ILoadingIndicator loadingIndicator,
        IAnalyticsEventBuilder analyticsEventBuilder,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            $"Fetching files...",
            context => FetchAsync(
                host,
                input,
                logger,
                context,
                deploymentDefinitionService,
                analyticsEventBuilder,
                cancellationToken));
    }

    internal static async Task FetchAsync(
        IHost host,
        FetchInput input,
        ILogger logger,
        StatusContext? loadingContext,
        ICliDeploymentDefinitionService definitionService,
        IAnalyticsEventBuilder analyticsEventBuilder,
        CancellationToken cancellationToken)
    {
        var services = host.Services.GetServices<IFetchService>().ToList();

        var supportedServicesStr = string.Join(", ", services.Select(s => s.ServiceType));
        var supportedServiceNamesStr = string.Join(", ", services.Select(s => s.ServiceName));

        bool areAllServicesSupported = !AreAllServicesSupported(input, services, out var unsupportedServicesStr);
        if (string.IsNullOrEmpty(input.Path) || areAllServicesSupported)
        {
            if (areAllServicesSupported)
            {
                logger.LogError($"These service options were not recognized: {unsupportedServicesStr}.");
            }

            logger.LogInformation($"Currently supported services are: {supportedServicesStr}." +
                $"{Environment.NewLine}    You can filter your service(s) with the --services option: " +
                $"{supportedServiceNamesStr}");
        }

        if (input.Reconcile)
        {
            if (input.Services.Count == 0)
            {
                logger.LogError(
                    "Reconcile is a destructive operation. Specify your service(s) with the --services option: {SupportedServiceNamesStr}",
                    supportedServiceNamesStr);
                return;
            }

            if (Path.GetExtension(input.Path) == CliDeploymentDefinitionService.Extension)
            {
                logger.LogError("Reconcile is not compatible with Deployment Definitions");
                return;
            }
        }

        var fetchResult = Array.Empty<FetchResult>();

        var fetchServices = services
            .Where(s => CheckService(input, s))
            .ToList();

        var inputPaths = new List<string> { input.Path };


        var ddefResult = GetDdefResult(
            definitionService,
            logger,
            inputPaths,
            fetchServices.Select(ds => ds.FileExtension));

        if (ddefResult == null)
        {
            return;
        }

        analyticsEventBuilder.SetAuthoringCommandlinePathsInputCount(new[] { input.Path });

        foreach (var fetchService in fetchServices)
        {
            analyticsEventBuilder.AddAuthoringServiceProcessed(fetchService.ServiceName);
        }

        var tasks = fetchServices
            .Select(
                m => m.FetchAsync(
                    input,
                    ddefResult.AllFilesByExtension[m.FileExtension],
                    loadingContext,
                    cancellationToken))
            .ToArray();

        try
        {
            fetchResult = await Task.WhenAll(tasks);
        }
        catch
        {
            // do nothing
            // this allows us to capture all the exceptions
            // and handle them below
        }

        var totalResult = new FetchResult(fetchResult, input.DryRun);

        if (input.IsJson)
        {
            var tableResult = new TableContent()
            {
                IsDryRun = input.DryRun
            };

            foreach (var task in tasks)
            {
                tableResult.AddRows(task.Result.ToTable());
            }

            logger.LogResultValue(tableResult);
        }
        else
        {
            logger.LogResultValue(totalResult);
        }

        if (ddefResult.DefinitionFiles.HasExcludes)
        {
            logger.LogInformation(ddefResult.GetExclusionsLogMessage());
        }

        // Get Exceptions from faulted deployments
        var exceptions = tasks
            .Where(t => t.IsFaulted)
            .Select(t => t.Exception!.InnerException)
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

    static bool CheckService(FetchInput input, IFetchService service)
    {
        if (!input.Reconcile && input.Services.Count == 0)
            return true;

        return input.Services.Contains(service.ServiceName);
    }

    static bool AreAllServicesSupported(FetchInput input, IReadOnlyList<IFetchService> services, out string unsupportedServices)
    {
        var serviceNames = services.Select(s => s.ServiceName);

        unsupportedServices = string.Join(", ", input.Services.Except(serviceNames));

        return string.IsNullOrEmpty(unsupportedServices);
    }

    static IDeploymentDefinitionFilteringResult? GetDdefResult(
        ICliDeploymentDefinitionService ddefService,
        ILogger logger,
        IEnumerable<string> inputPaths,
        IEnumerable<string> extensions)
    {
        IDeploymentDefinitionFilteringResult? ddefResult = null;
        try
        {
            ddefResult = ddefService
                .GetFilesFromInput(inputPaths, extensions);
        }
        catch (MultipleDeploymentDefinitionInDirectoryException e)
        {
            logger.LogError(e.Message);
        }
        catch (DeploymentDefinitionFileIntersectionException e)
        {
            logger.LogError(e.Message);
        }

        return ddefResult;
    }
}
