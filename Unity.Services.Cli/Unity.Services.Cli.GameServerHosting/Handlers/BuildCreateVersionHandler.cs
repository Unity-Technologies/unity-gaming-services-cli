using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Service;
using static Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.CreateBuild200Response.BuildTypeEnum;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static partial class BuildCreateVersionHandler
{
    public static async Task BuildCreateVersionAsync(
        BuildCreateVersionInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        HttpClient httpClient,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync(
            "Creating build version...",
            _ => BuildCreateVersionAsync(
                input,
                unityEnvironment,
                service,
                logger,
                httpClient,
                cancellationToken: cancellationToken
            ));
    }

    internal static async Task BuildCreateVersionAsync(
        BuildCreateVersionInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        HttpClient httpClient,
        CancellationToken cancellationToken = default
    )
    {
        httpClient.Timeout = TimeSpan.FromMinutes(10);

        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var buildIdStr = input.BuildId ?? throw new MissingInputException(BuildCreateVersionInput.BuildIdKey);
        var buildId = long.Parse(buildIdStr);

        await service.AuthorizeGameServerHostingService(cancellationToken);

        var build = await service.BuildsApi.GetBuildAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            buildId,
            cancellationToken: cancellationToken);

        switch (build.BuildType)
        {
            case CONTAINER:
                await CreateContainerBuildVersion(
                    logger,
                    service,
                    input,
                    environmentId,
                    build,
                    cancellationToken);
                break;
            case FILEUPLOAD:
                await CreateFileUploadBuildVersion(
                    logger,
                    service,
                    input,
                    environmentId,
                    build,
                    httpClient,
                    cancellationToken
                );
                break;
            case S3:
                await CreateBucketUploadBuildVersion(
                    logger,
                    service,
                    input,
                    environmentId,
                    build,
                    cancellationToken);
                break;
            default:
                throw new CliException("Unsupported build type", ExitCode.HandledError);
        }
    }
}
