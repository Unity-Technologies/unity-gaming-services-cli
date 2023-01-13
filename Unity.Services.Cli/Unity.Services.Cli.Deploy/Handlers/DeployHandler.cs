using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Deploy.Input;
using Unity.Services.Cli.Deploy.Model;

namespace Unity.Services.Cli.Deploy.Handlers;

static class DeployHandler
{
    public static async Task DeployAsync(
        IHost host,
        DeployInput input,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
        )
    {
        await loadingIndicator.StartLoadingAsync($"Deploying files...",
            context => DeployAsync(
                host,
                input,
                logger,
                context,
                cancellationToken));
    }

    internal static async Task DeployAsync(
        IHost host,
        DeployInput input,
        ILogger logger,
        StatusContext? loadingContext,
        CancellationToken cancellationToken
    )
    {
        var services = host.Services.GetServices<IDeploymentService>().ToList();

        if (input.Paths.Count == 0)
        {
            var supportedServicesStr = string.Join(", ", services.Select(s => s.ServiceType));
            logger.LogInformation("Currently supported services are: {SupportedServicesStr}", supportedServicesStr);
        }

        var tasks = services.Select(m => m.Deploy(input, loadingContext, cancellationToken)).ToArray();
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception)
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
            deploymentResults.SelectMany(x => x.Deployed).ToList(),
            deploymentResults.SelectMany(x => x.Failed).ToList()
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
}
