using System.Net;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Client;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class CoreDumpGetHandler
{
    public static async Task CoreDumpGetAsync(
        FleetIdInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching core dump configuration...",
            _ => CoreDumpGetAsync(
                input,
                unityEnvironment,
                service,
                logger,
                cancellationToken));
    }

    internal static async Task CoreDumpGetAsync(
        FleetIdInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var fleetId = input.FleetId ?? throw new MissingInputException(FleetIdInput.FleetIdKey);

        await service.AuthorizeGameServerHostingService(cancellationToken);

        try
        {
            var coreDump = await service.CoreDumpApi.GetCoreDumpConfigAsync(
                Guid.Parse(input.CloudProjectId!),
                Guid.Parse(environmentId),
                Guid.Parse(fleetId),
                operationIndex: 0,
                cancellationToken: cancellationToken
            );

            logger.LogResultValue(new CoreDumpOutput(coreDump));
        }
        catch (ApiException e) when (e.ErrorCode == (int)HttpStatusCode.NotFound)
        {
            throw new CliException(
                "Core Dump Storage is not configured for this fleet. Try `create` command to configure it.",
                e,
                ExitCode.HandledError);
        }
        catch (ApiException e) when (e.ErrorCode == (int)HttpStatusCode.BadRequest)
        {
            ApiExceptionConverter.Convert(e);
        }
    }
}
