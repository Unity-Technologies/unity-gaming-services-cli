using System.Net;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Client;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class CoreDumpCreateHandler
{
    public static async Task CoreDumpCreateAsync(
        CoreDumpCreateInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        GcsCredentialParser gcsCredentialParser,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Creating core dump config...",
            _ => CoreDumpCreateAsync(
                input,
                unityEnvironment,
                service,
                logger,
                gcsCredentialParser,
                cancellationToken));
    }

    internal static async Task CoreDumpCreateAsync(
        CoreDumpCreateInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        GcsCredentialParser gcsCredentialParser,
        CancellationToken cancellationToken)
    {
        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var fleetId = input.FleetId ?? throw new MissingInputException(FleetIdInput.FleetIdKey);

        await service.AuthorizeGameServerHostingService(cancellationToken);

        if (!Enum.TryParse(input.StorageType, true, out CreateCoreDumpConfigRequest.StorageTypeEnum storageType))
        {
            throw new ArgumentException(
                $"Invalid storage type {input.StorageType}",
                nameof(CoreDumpUpdateInput.StorageType));
        }

        try
        {
            var credentials = gcsCredentialParser.Parse(input.CredentialsFile);
            var coreDumpCreateResponse = await service.CoreDumpApi.PostCoreDumpConfigAsync(
                Guid.Parse(input.CloudProjectId!),
                Guid.Parse(environmentId),
                Guid.Parse(fleetId),
                new CreateCoreDumpConfigRequest(
                    new CredentialsForTheBucket1(
                        credentials.ClientEmail,
                        credentials.PrivateKey,
                        input.GcsBucket
                    ),
                    0,
                    CoreDumpStateConverter.ConvertStringToCreateStateEnum(input.State),
                    storageType
                ),
                0,
                cancellationToken);

            logger.LogResultValue(new CoreDumpOutput(coreDumpCreateResponse));
        }
        catch (ApiException e) when (e.ErrorCode == (int)HttpStatusCode.BadRequest)
        {
            ApiExceptionConverter.Convert(e);
        }
    }
}
