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

namespace Unity.Services.Cli.Authoring.Handlers;

static class FetchHandler
{
    public static async Task FetchAsync(
        IHost host,
        FetchInput input,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            $"Fetching files...",
            context => FetchAsync(
                host,
                input,
                logger,
                context,
                cancellationToken));
    }

    internal static async Task FetchAsync(
        IHost host,
        FetchInput input,
        ILogger logger,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        var services = host.Services.GetServices<IFetchService>().ToList();

        if (string.IsNullOrEmpty(input.Path))
        {
            var supportedServicesStr = string.Join(", ", services.Select(s => s.ServiceType));
            logger.LogInformation("Currently supported services are: {supportedServicesStr}", supportedServicesStr);
        }

        var fetchResult = Array.Empty<FetchResult>();
        Task<FetchResult[]>? fetchAll = null;
        try
        {
            fetchAll = Task.WhenAll(
                services.Select(
                    m => m.FetchAsync(
                        input,
                        loadingContext,
                        cancellationToken)));
            fetchResult = await fetchAll;
        }
        catch
        {
            // will use fetchAll to find all errors
        }

        var totalResult = new FetchResult(fetchResult);
        logger.LogResultValue(totalResult);

        if (fetchAll!.IsFaulted)
        {
            HandleFailedFetch();
        }

        if (totalResult.Failed.Any())
        {
            throw new CliException("One or more files failed to be fetched", ExitCode.HandledError);
        }

        void HandleFailedFetch()
        {
            var exception = fetchAll.Exception!;
            if (exception.InnerExceptions.Count == 1)
            {
                var innerException = exception.InnerException!;
                var exitCode = innerException is CliException cliException
                    ? cliException.ExitCode
                    : ExitCode.UnhandledError;
                throw new CliException(innerException.Message, innerException, exitCode);
            }

            var errorMessage = "Failed to fetch due to the following errors:"
                + string.Join($"{Environment.NewLine}    - ", exception.InnerExceptions.Select(x => x.Message));
            var cliExceptions = exception.InnerExceptions.OfType<CliException>().ToList();
            if (cliExceptions.Count == exception.InnerExceptions.Count)
            {
                throw new CliException(errorMessage, fetchAll.Exception!, cliExceptions.Max(x => x.ExitCode));
            }

            throw new CliException(errorMessage, exception, ExitCode.UnhandledError);
        }
    }
}
