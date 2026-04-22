using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Triggers.Input;
using Unity.Services.Cli.Triggers.Model;
using Unity.Services.Cli.Triggers.Service;

namespace Unity.Services.Cli.Triggers.Handlers;

static class ListTriggersHandler
{
    public static async Task ListAsync(
        ListTriggersInput input,
        IUnityEnvironment unityEnvironment,
        ITriggersService triggersService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching trigger list...",
            _ => ListAsync(input, unityEnvironment, triggersService, logger, cancellationToken));
    }

    internal static async Task ListAsync(
        ListTriggersInput input,
        IUnityEnvironment unityEnvironment,
        ITriggersService triggersService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var triggers = await triggersService.GetTriggersAsync(
            projectId, environmentId, input.Limit, cancellationToken);

        var result = new ListTriggersOutput(triggers);
        logger.LogResultValue(result);
    }
}
