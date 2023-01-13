using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Environment.Input;

namespace Unity.Services.Cli.Environment.Handlers;

static class ListHandler
{
    public static async Task ListAsync(EnvironmentInput input, IEnvironmentService environmentService, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Fetching environments...",  _ =>
            ListAsync(input, environmentService, logger, cancellationToken));
    }

    internal static async Task ListAsync(EnvironmentInput input, IEnvironmentService environmentService,
        ILogger logger, CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId ?? throw new MissingConfigurationException(
            Keys.ConfigKeys.ProjectId, Keys.EnvironmentKeys.ProjectId);

        var environments = await environmentService
            .ListAsync(projectId, cancellationToken);

        if (input.IsJson)
        {
            logger.LogResultValue(environments);
        }
        else
        {
            var environmentNames = environments
                .Select(e => $"\"{e.Name}\": \"{e.Id}\"");
            logger.LogResultValue(environmentNames);
        }
    }
}
