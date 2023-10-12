using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Cli.GameServerHosting.Service;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class MachineListHandler
{
    public static async Task MachineListAsync(
        MachineListInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync("Fetching machine list...",
            _ => MachineListAsync(input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task MachineListAsync(
        MachineListInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

        Guid? fleetId = null;
        string? locationId = null;
        string? hardwareType = null;
        string? partial = null;
        string? status = null;

        if (input.FleetId != null)
        {
            fleetId = Guid.Parse(input.FleetId);
        }

        if (input.LocationId != null)
        {
            locationId = input.LocationId;
        }

        if (input.HardwareType != null)
        {
            hardwareType = input.HardwareType;
        }

        if (input.Partial != null)
        {
            partial = input.Partial;
        }

        if (input.Status != null)
        {
            status = input.Status;
        }


        await service.AuthorizeGameServerHostingService(cancellationToken);

        var machines = await service.MachinesApi.ListMachinesAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            fleetId: fleetId,
            locationId: locationId,
            hardwareType: hardwareType,
            partial: partial,
            status: status,
            cancellationToken: cancellationToken
        );

        logger.LogResultValue(new MachinesOutput(machines));
    }
}
