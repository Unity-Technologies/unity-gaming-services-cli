using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class FileDownloadHandler
{
    public static async Task FileDownloadAsync(
        FileDownloadInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        HttpClient httpClient,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync(
            "Downloading file...",
            _ => FileDownloadAsync(
                input,
                unityEnvironment,
                service,
                logger,
                httpClient,
                cancellationToken));
    }

    internal static async Task FileDownloadAsync(
        FileDownloadInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        HttpClient httpClient,
        CancellationToken cancellationToken
    )
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var output = input.Output ?? throw new MissingInputException(FileDownloadInput.OutputKey);

        await service.AuthorizeGameServerHostingService(cancellationToken);

        var request = new GenerateContentURLRequest(
            path: input.Path!,
            serverId: long.Parse(input.ServerId!)
        );

        var response = await service.FilesApi.GenerateDownloadURLAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            request,
            cancellationToken: cancellationToken
        );

        // check if output is a directory or a file
        if (Directory.Exists(output))
        {
            // get filename from path
            var filename = Path.GetFileName(input.Path!);

            // create local file
            output = Path.Combine(output, filename);
        }

        // create local file
        var localFile = new FileStream(output, FileMode.Create);

        try
        {
            // stream file from signed url
            var stream = await httpClient.GetStreamAsync(response.Url, cancellationToken);

            // write file to local file
            await stream.CopyToAsync(localFile, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError("Failed to download file: {Message}", exception.Message);
            return;
        }
        finally
        {
            // close file
            localFile.Close();
        }


        logger.LogInformation("File downloaded to {Output}", input.Output);
    }
}
