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

static class FetchCommandHandler
{
    public static async Task FetchAsync(
        IHost host,
        FetchInput input,
        IUnityEnvironment unityEnvironment,
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
                unityEnvironment,
                logger,
                context,
                deploymentDefinitionService,
                analyticsEventBuilder,
                cancellationToken));
    }

    internal static async Task FetchAsync(
        IHost host,
        FetchInput input,
        IUnityEnvironment unityEnvironment,
        ILogger logger,
        StatusContext? loadingContext,
        ICliDeploymentDefinitionService definitionService,
        IAnalyticsEventBuilder analyticsEventBuilder,
        CancellationToken cancellationToken)
    {
        var inputPaths = new List<string>();
        if (!string.IsNullOrEmpty(input.Path))
        {
            inputPaths.Add(input.Path);
        }

        var services = host.Services.GetServices<IFetchService>().ToList();

        if (!AuthoringHandlerCommon.PreActionValidation(
                input,
                logger,
                services,
                inputPaths))
        {
            return;
        }

        var fetchResult = Array.Empty<FetchResult>();

        var fetchServices = services
            .Where(s => AuthoringHandlerCommon.CheckService(input, s.ServiceName))
            .ToArray();

        var ddefResult = AuthoringHandlerCommon.GetDdefResult(
            definitionService,
            logger,
            inputPaths,
            fetchServices.SelectMany(ds => ds.FileExtensions));

        if (ddefResult == null)
        {
            return;
        }

        AuthoringHandlerCommon.SendAnalytics(analyticsEventBuilder, inputPaths, fetchServices);

        var projectId = input.CloudProjectId!;
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

        var authoringResultServiceTask = fetchServices
            .Select<IFetchService, AuthoringResultServiceTask<FetchResult>>(
                service =>
                {
                    var filePaths = service.FileExtensions
                        .SelectMany(extension => ddefResult.AllFilesByExtension[extension])
                        .ToArray();

                    if (!input.Reconcile && !filePaths.Any())
                    {
                        // nothing to do for this service
                        return new AuthoringResultServiceTask<FetchResult>(
                            Task.FromResult(new FetchResult(Array.Empty<AuthorResult>())),
                            service.ServiceType);
                    }

                    return new AuthoringResultServiceTask<FetchResult>(
                        service.FetchAsync(
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
            fetchResult = await Task.WhenAll(
                authoringResultServiceTask
                    .Select(t => t.AuthorResultTask));
        }
        catch
        {
            // do nothing
            // this allows us to capture all the exceptions
            // and handle them below
        }

        var totalResult = new FetchResult(fetchResult, input.DryRun);

        AuthoringHandlerCommon.PrintResult(
            input,
            logger,
            authoringResultServiceTask,
            totalResult,
            ddefResult);
    }
}
