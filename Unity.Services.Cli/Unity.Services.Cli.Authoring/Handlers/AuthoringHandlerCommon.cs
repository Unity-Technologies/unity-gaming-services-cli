using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Model.TableOutput;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent;

namespace Unity.Services.Cli.Authoring.Handlers;

static class AuthoringHandlerCommon
{
    public static bool PreActionValidation<T>(
        AuthoringInput input,
        ILogger logger,
        IReadOnlyList<T> services,
        IReadOnlyList<string> inputPaths)
        where T : IAuthoringService
    {
        var supportedServicesStr = string.Join(", ", services.Select(s => s.ServiceType));
        var serviceNames = services.Select(s => s.ServiceName).ToList();
        var supportedServiceNamesStr = string.Join(", ", serviceNames);

        bool areAllServicesSupported = !AreAllServicesSupported(
            input,
            serviceNames,
            out var unsupportedServicesStr);

        if (inputPaths.Count == 0 || areAllServicesSupported)
        {
            if (areAllServicesSupported)
            {
                logger.LogError($"These service options were not recognized: {unsupportedServicesStr}.");
            }

            logger.LogInformation(
                $"Currently supported services are: {supportedServicesStr}." +
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
                return false;
            }

            if (typeof(T) == typeof(IFetchService)
                && inputPaths.Count > 0
                && Path.GetExtension(inputPaths[0]) == CliDeploymentDefinitionService.Extension)
            {
                logger.LogError("Reconcile Fetch is not compatible with Deployment Definitions");
                return false;
            }
        }

        return true;
    }

    public static void SendAnalytics<T>(
        IAnalyticsEventBuilder analyticsEventBuilder,
        IReadOnlyList<string> inputPaths,
        T[] deploymentServices) where T : IAuthoringService
    {
        analyticsEventBuilder.SetAuthoringCommandlinePathsInputCount(inputPaths);

        foreach (var deploymentService in deploymentServices)
        {
            analyticsEventBuilder.AddAuthoringServiceProcessed(deploymentService.ServiceName);
        }
    }

    public static IDeploymentDefinitionFilteringResult? GetDdefResult(
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

    public static bool CheckService(AuthoringInput input, string serviceName)
    {
        if (!input.Reconcile && input.Services.Count == 0)
            return true;

        return input.Services.Contains(serviceName);
    }

    public static bool AreAllServicesSupported(AuthoringInput input, IReadOnlyList<string> serviceNames, out string unsupportedServices)
    {
        unsupportedServices = string.Join(", ", input.Services.Except(serviceNames));

        return string.IsNullOrEmpty(unsupportedServices);
    }

    public static void PrintResult<T>(
        AuthoringInput input,
        ILogger logger,
        AuthoringResultServiceTask<T>[] tasks,
        T totalResult,
        IDeploymentDefinitionFilteringResult ddefResult) where T : AuthorResult
    {
        if (input.IsJson)
        {
            var tableResult = new TableContent()
            {
                IsDryRun = input.DryRun
            };

            foreach (var task in tasks)
            {
                tableResult.AddRows(task.AuthorResultTask.Result.ToTable(task.ServiceType));
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
            .Select(t => t.AuthorResultTask)
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
}
