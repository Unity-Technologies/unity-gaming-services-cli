using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Deploy.Input;
using Unity.Services.Cli.Deploy.Model;
using Unity.Services.Cli.Deploy.Service;

namespace Unity.Services.Cli.Deploy.Handlers;

static class FetchHandler
{
    public static async Task FetchAsync(
        IHost host,
        FetchInput input,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
        )
    {
        await loadingIndicator.StartLoadingAsync($"Fetching files...",
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
                services.Select(m => m.FetchAsync(
                    input,
                    loadingContext,
                    cancellationToken)));
            fetchResult = await fetchAll;
        }
        catch { /* will use fetchAll to find all errors */ }

        var totalResult = new FetchResult(fetchResult);

        logger.LogResultValue(totalResult);

        if (fetchAll!.IsFaulted)
        {
            var exception = fetchAll.Exception;
            if (exception != null
                && exception.InnerExceptions.Count == 1
                && exception.InnerException is CliException cliException)
            {
                throw new CliException(cliException.Message, cliException, cliException.ExitCode);
            }
            throw new CliException("Failed to fetch due to an unexpected error", fetchAll.Exception!, ExitCode.UnhandledError);
        }

        if (totalResult.Failed.Any())
        {
            throw new CliException($"One or more files failed to be fetched", ExitCode.HandledError);
        }
    }
}
