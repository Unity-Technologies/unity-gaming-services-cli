using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Triggers.Input;
using Unity.Services.Cli.Triggers.Model;
using Unity.Services.Cli.Triggers.Service;

namespace Unity.Services.Cli.Triggers.Handlers;

static class GetTriggerHandler
{
    public static async Task GetAsync(
        GetTriggerInput input,
        IUnityEnvironment unityEnvironment,
        ITriggersService triggersService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching trigger...",
            _ => GetAsync(input, unityEnvironment, triggersService, logger, cancellationToken));
    }

    internal static async Task GetAsync(
        GetTriggerInput input,
        IUnityEnvironment unityEnvironment,
        ITriggersService triggersService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var trigger = await triggersService.GetTriggerAsync(
            projectId, environmentId, input.TriggerId!, cancellationToken);

        logger.LogResultValue(new GetTriggerOutput(trigger));
    }
}
