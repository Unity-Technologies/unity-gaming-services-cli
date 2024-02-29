using System.Net;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Client;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class CoreDumpDeleteHandler
{
    public static async Task CoreDumpDeleteAsync(
        FleetIdInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync(
            "Deleting core dump config...",
            _ => CoreDumpDeleteAsync(
                input,
                unityEnvironment,
                service,
                logger,
                cancellationToken));
    }

    internal static async Task CoreDumpDeleteAsync(
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
            await service.CoreDumpApi.DeleteCoreDumpConfigAsync(
                Guid.Parse(input.CloudProjectId!),
                Guid.Parse(environmentId),
                Guid.Parse(fleetId),
                operationIndex: 0,
                cancellationToken: cancellationToken
            );

            logger.LogInformation("Core Dump config deleted successfully");
        }
        catch (ApiException ex) when (ex.ErrorCode == (int)HttpStatusCode.NotFound)
        {
            throw new CliException(
                "Core Dump Storage is not configured for this fleet. Try `create` command to configure it.",
                ex,
                ExitCode.HandledError);
        }
        catch (ApiException e) when (e.ErrorCode == (int)HttpStatusCode.BadRequest)
        {
            ApiExceptionConverter.Convert(e);
        }
    }
}
