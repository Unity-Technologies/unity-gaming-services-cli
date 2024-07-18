using System.IO.Abstractions;
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
    public static IFile FileSystem { get; set; } = new FileSystem().File;

    static async Task CreateGcsUploadBuildVersion(
        IGameServerHostingService service,
        BuildCreateVersionInput input,
        string environmentId,
        ILogger logger,
        CreateBuild200Response build,
        CancellationToken cancellationToken)
    {
        ValidateGcsInput(input);

        var serviceAccountFileContent = await ReadServiceAccountFileAsync(
            input.ServiceAccountJsonFile!,
            FileSystem,
            cancellationToken);

        try
        {
            await service.BuildsApi.CreateNewBuildVersionAsync(
                Guid.Parse(input.CloudProjectId!),
                Guid.Parse(environmentId),
                build.BuildID,
                new CreateNewBuildVersionRequest(
                    buildVersionName: input.BuildVersionName!,
                    gcs: new GoogleCloudStorageRequest(
                        input.BucketUrl!,
                        serviceAccountFileContent
                    )
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
    static void ValidateGcsInput(BuildCreateVersionInput input)
    {
        if (input.ServiceAccountJsonFile == null)
            throw new MissingInputException(BuildCreateVersionInput.ServiceAccountJsonFileKey);
        if (input.BucketUrl == null)
            throw new MissingInputException(BuildCreateVersionInput.BucketUrlKey);
        if (input.SecretKey != null)
            throw new CliException("Build does not support s3 flags.", ExitCode.HandledError);
        if (input.AccessKey != null)
            throw new CliException("Build does not support s3 flags.", ExitCode.HandledError);
        if (input.ContainerTag != null)
            throw new CliException("Build does not support container flags.", ExitCode.HandledError);
        if (input.FileDirectory != null)
            throw new CliException("Build does not support file upload flags.", ExitCode.HandledError);
    }

    static async Task<string> ReadServiceAccountFileAsync(
        string serviceAccountJsonFile,
        IFile fileSystem,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(serviceAccountJsonFile);
        try
        {
            return await fileSystem.ReadAllTextAsync(fullPath, cancellationToken);
        }
        catch (IOException e)
        {
            throw new CliException(
                $"Failed to read service account file at {fullPath}. IO error: {e.Message}",
                e,
                ExitCode.HandledError);
        }
    }
}
