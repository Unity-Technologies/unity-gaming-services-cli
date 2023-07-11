using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static partial class BuildCreateVersionHandler
{
    static async Task CreateContainerBuildVersion(
        ILogger logger,
        IGameServerHostingService service,
        BuildCreateVersionInput input,
        string environmentId,
        CreateBuild200Response build,
        CancellationToken cancellationToken
    )
    {
        ValidateContainerInput(input);
        await service.BuildsApi.CreateNewBuildVersionAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            build.BuildID,
            new CreateNewBuildVersionRequest(container: new ContainerImage(input.ContainerTag!)),
            cancellationToken: cancellationToken
        );
        logger.LogInformation("Build version created successfully");
    }

    // We need to apply our own conditional validation based on the build type
    internal static void ValidateContainerInput(BuildCreateVersionInput input)
    {
        if (input.ContainerTag == null)
            throw new MissingInputException(BuildCreateVersionInput.ContainerTagKey);
        if (input.FileDirectory != null)
            throw new CliException("Build does not support file upload flags.", ExitCode.HandledError);
        if (input.BucketUrl != null)
            throw new CliException("Build does not support s3 buckets.", ExitCode.HandledError);
    }
}
