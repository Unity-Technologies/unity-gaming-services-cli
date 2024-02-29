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

static class BuildCreateHandler
{
    public static async Task BuildCreateAsync(
        BuildCreateInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync(
            "Creating build...",
            _ =>
                BuildCreateAsync(
                    input,
                    unityEnvironment,
                    service,
                    logger,
                    cancellationToken));
    }

    internal static async Task BuildCreateAsync(
        BuildCreateInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var buildName = input.BuildName ?? throw new MissingInputException(BuildCreateInput.NameKey);
        var buildOsFamily = input.BuildOsFamily ?? throw new MissingInputException(BuildCreateInput.OsFamilyKey);
        var buildType = input.BuildType ?? throw new MissingInputException(BuildCreateInput.TypeKey);

        await service.AuthorizeGameServerHostingService(cancellationToken);

        try
        {
            var createBuildRequest = new CreateBuildRequest(
                buildName,
                buildType,
                osFamily: buildOsFamily);

            if (input.BuildVersionName != null)
            {
                createBuildRequest.BuildVersionName = input.BuildVersionName;
            }

            var build = await service.BuildsApi.CreateBuildAsync(
                Guid.Parse(input.CloudProjectId!),
                Guid.Parse(environmentId),
                createBuildRequest,
                cancellationToken: cancellationToken);

            logger.LogResultValue(new BuildOutput(build));
        }
        catch (ApiException e)
        {
            ApiExceptionConverter.Convert(e);
        }
    }
}
