using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Environment.Input;

namespace Unity.Services.Cli.Environment.Handlers;

static class AdditionHandler
{
    public static async Task AddAsync(EnvironmentInput environmentInput, IEnvironmentService environmentService,
        ILogger logger, ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Adding environment...", _ =>
            AddAsync(environmentInput, environmentService, logger, cancellationToken));
    }

    internal static async Task AddAsync(EnvironmentInput environmentInput, IEnvironmentService environmentService,
        ILogger logger, CancellationToken cancellationToken)
    {
        var environment = environmentInput.EnvironmentName;
        var projectId = environmentInput.CloudProjectId ?? throw new MissingConfigurationException(
            Keys.ConfigKeys.ProjectId, Keys.EnvironmentKeys.ProjectId);

        await environmentService.AddAsync(environment!, projectId, cancellationToken);
        logger.LogInformation("'{environment}' added", environment);
    }
}
