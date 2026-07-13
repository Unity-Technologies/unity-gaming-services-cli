using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Utils;
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
        IAuthoringFileService deploymentDefinitionService,
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
        IAuthoringFileService authoringFileService,
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
            authoringFileService,
            logger,
            inputPaths,
            fetchServices.SelectMany(ds => ds.FileExtensions).ToList());

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
                    var authoringFiles = service.FileExtensions
                        .SelectMany(extension => ddefResult.AllFilesByExtension[extension])
                        .ToArray();

                    if (!input.Reconcile && !authoringFiles.Any())
                    {
                        // nothing to do for this service
                        return new AuthoringResultServiceTask<FetchResult>(
                            Task.FromResult(new FetchResult(Array.Empty<AuthorResult>())),
                            service.ServiceType);
                    }

                    var targetVariantTags = FilterByDDef(ddefResult, input, ref authoringFiles, logger);

                    AuthoringHandlerCommon.ReconcileVariantTags(input, authoringFiles, targetVariantTags);

                    return new AuthoringResultServiceTask<FetchResult>(
                        service.FetchAsync(
                            input,
                            authoringFiles,
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

    /// <summary>
    /// Applies filtering to authoring files based on the input path's associated deployment definition.
    /// </summary>
    static IReadOnlyList<string> FilterByDDef(
        IDeploymentDefinitionFilteringResult ddefResult,
        FetchInput input,
        ref AuthoringFile[] authoringFiles,
        ILogger logger)
    {
        ddefResult.DeploymentDefinitionByInputPath.TryGetValue(input.Path, out var ddef);

        var absoluteDdefPath = ddef?.Path != null ? Path.GetFullPath(ddef.Path) : null;

        var lookup = authoringFiles.ToLookup(f =>
        {
            var fileAbsolutePath = f.DeploymentDefinition?.Path != null
                ? Path.GetFullPath(f.DeploymentDefinition.Path)
                : null;

            return Equals(fileAbsolutePath, absoluteDdefPath);
        });

        authoringFiles = lookup[true].ToArray();
        var filteredOutFiles = lookup[false].ToArray();

        LogIgnoredDeploymentDefinition(filteredOutFiles, logger);

        return VariantTagsUtils.FromAdditionalProperties(ddef?.AdditionalProperties);
    }

    static void LogIgnoredDeploymentDefinition(AuthoringFile[] ignoredFile, ILogger logger)
    {
        var ignoredDdefPaths = ignoredFile
            .Select(f => f.DeploymentDefinition?.Path)
            .Where(path => !string.IsNullOrEmpty(path))
            .Distinct()
            .Cast<string>()
            .ToList();

        if (ignoredDdefPaths.Count != 0)
        {
            var relativePaths = ignoredDdefPaths.Select(path =>
                Path.GetRelativePath(Directory.GetCurrentDirectory(), path));
            var pathsList = string.Join($"{Environment.NewLine}  - ", relativePaths);

            logger.LogInformation(
                $"The files associated with the following deployment definition were ignored:{Environment.NewLine}  - {pathsList}{Environment.NewLine}" +
                "To fetch these files, run fetch on the parent folder of each deployment definition.");
        }
    }
}
