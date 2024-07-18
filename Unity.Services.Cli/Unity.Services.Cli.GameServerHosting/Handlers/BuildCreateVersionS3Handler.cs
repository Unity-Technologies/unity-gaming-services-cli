using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Client;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static partial class BuildCreateVersionHandler
{
    static async Task CreateS3UploadBuildVersion(
        ILogger logger,
        IGameServerHostingService service,
        BuildCreateVersionInput input,
        string environmentId,
        CreateBuild200Response build,
        CancellationToken cancellationToken
    )
    {
        ValidateS3Input(input);
        try
        {
            await service.BuildsApi.CreateNewBuildVersionAsync(
                Guid.Parse(input.CloudProjectId!),
                Guid.Parse(environmentId),
                build.BuildID,
                new CreateNewBuildVersionRequest(
                    buildVersionName: input.BuildVersionName!,
                    s3: new AmazonS3Request(input.AccessKey!, input.BucketUrl!, input.SecretKey!)
                ),
                cancellationToken: cancellationToken
            );

            logger.LogInformation("Build version created successfully");
        }
        catch (ApiException e)
        {
            ApiExceptionConverter.Convert(e);
        }
    }

    // We need to apply our own conditional validation based on the build type
    internal static void ValidateS3Input(BuildCreateVersionInput input)
    {
        if (input.AccessKey == null)
            throw new MissingInputException(BuildCreateVersionInput.AccessKeyKey);
        if (input.BucketUrl == null)
            throw new MissingInputException(BuildCreateVersionInput.BucketUrlKey);
        if (input.SecretKey == null)
            throw new MissingInputException(BuildCreateVersionInput.SecretKeyKey);
        if (input.ServiceAccountJsonFile != null)
            throw new CliException("Build does not support gcs flags.", ExitCode.HandledError);
        if (input.ContainerTag != null)
            throw new CliException("Build does not support container flags.", ExitCode.HandledError);
        if (input.FileDirectory != null)
            throw new CliException("Build does not support file upload flags.", ExitCode.HandledError);
    }
}
