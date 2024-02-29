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

static class CoreDumpUpdateHandler
{
    public static async Task CoreDumpUpdateAsync(
        CoreDumpUpdateInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        GcsCredentialParser gcsCredentialParser,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Updating core dump config...",
            _ => CoreDumpUpdateAsync(
                input,
                unityEnvironment,
                service,
                logger,
                gcsCredentialParser,
                cancellationToken));
    }

    internal static async Task CoreDumpUpdateAsync(
        CoreDumpUpdateInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        GcsCredentialParser gcsCredentialParser,
        CancellationToken cancellationToken)
    {
        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var fleetId = input.FleetId ?? throw new MissingInputException(FleetIdInput.FleetIdKey);

        var request = new UpdateCoreDumpConfigRequest();

        if (input.StorageType != null)
        {
            if (!Enum.TryParse(input.StorageType, true, out UpdateCoreDumpConfigRequest.StorageTypeEnum storageType))
            {
                throw new ArgumentException(
                    $"Invalid storage type {input.StorageType}",
                    nameof(CoreDumpUpdateInput.StorageType));
            }

            request.StorageType = storageType;
        }

        if (input.State != null)
        {
            request.State = CoreDumpStateConverter.ConvertStringToUpdateStateEnum(input.State);
        }

        if (input.CredentialsFile != null || input.GcsBucket != null)
        {
            var credentials = new CredentialsForTheBucket1();
            if (input.CredentialsFile != null)
            {
                var parsedCredentials = gcsCredentialParser.Parse(input.CredentialsFile);
                credentials.ServiceAccountAccessId = parsedCredentials.ClientEmail;
                credentials.ServiceAccountPrivateKey = parsedCredentials.PrivateKey;
            }

            if (input.GcsBucket != null)
            {
                credentials.StorageBucket = input.GcsBucket;
            }

            request.Credentials = credentials;
        }

        await service.AuthorizeGameServerHostingService(cancellationToken);

        try
        {
            var coreDumpCreateResponse = await service.CoreDumpApi.PutCoreDumpConfigAsync(
                Guid.Parse(input.CloudProjectId!),
                Guid.Parse(environmentId),
                Guid.Parse(fleetId),
                request,
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
