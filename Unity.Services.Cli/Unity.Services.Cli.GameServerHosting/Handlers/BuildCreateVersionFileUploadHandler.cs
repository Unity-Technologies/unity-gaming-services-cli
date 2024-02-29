using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Polly;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Client;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using SystemFile = System.IO.File;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static partial class BuildCreateVersionHandler
{
    static readonly List<string> k_FilesToIgnore = new()
    {
        ".DS_Store"
    };

    static int Limit => 100;

    static async Task CreateFileUploadBuildVersion(
        ILogger logger,
        IGameServerHostingService service,
        BuildCreateVersionInput input,
        string environmentId,
        CreateBuild200Response build,
        HttpClient httpClient,
        CancellationToken cancellationToken
    )
    {
        if (build.SyncStatus == CreateBuild200Response.SyncStatusEnum.SYNCING)
            throw new CliException("Build is currently syncing. Please try again soon....", ExitCode.HandledError);

        var localFiles = GetLocalFiles(input.FileDirectory!, logger);

        if (!localFiles.Any())
        {
            logger.LogInformation("No files to upload");
            return;
        }

        var uploaded = await UploadFiles(
            service,
            input.CloudProjectId!,
            environmentId,
            build,
            localFiles,
            httpClient,
            cancellationToken);

        if (uploaded == 0)
        {
            logger.LogInformation("No files uploaded");
            return;
        }

        var deleted = 0;
        if (input.RemoveOldFiles ?? false)
            deleted = await DeleteOldFiles(
                service,
                input.CloudProjectId!,
                environmentId,
                build,
                localFiles,
                cancellationToken);

        var policy = Policy
            .Handle<ApiException>((exception => exception.ErrorCode.Equals(HttpStatusCode.BadRequest)))
            .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        try
        {
            await policy.ExecuteAsync(
                async () => await service.BuildsApi.CreateNewBuildVersionAsync(
                    Guid.Parse(input.CloudProjectId!),
                    Guid.Parse(environmentId),
                    build.BuildID,
                    new CreateNewBuildVersionRequest(
                        buildVersionName: input.BuildVersionName!,
                        ccd: new CCDDetails2(build.Ccd.BucketID)
                    ),
                    cancellationToken: cancellationToken
                ));
        }
        catch (ApiException e) when (e.ErrorCode == (int)HttpStatusCode.BadRequest)
        {
            ApiExceptionConverter.Convert(e);
        }

        var details = new StringBuilder()
            .AppendLine($"Files to upload: {localFiles.Count}")
            .AppendLine($"Files uploaded: {uploaded}")
            .AppendLine($"Files deleted: {deleted}")
            .ToString();

        logger.LogInformation("Build version created successfully\n{}", details);
    }

    static List<LocalFile> GetLocalFiles(string directory, ILogger logger)
    {
        try
        {
            var localFiles = Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                .Where(fullSystemPathFile => !k_FilesToIgnore.Any(fullSystemPathFile.Contains))
                .Select(
                    f =>
                    {
                        var pathWithInDir = f.Replace(directory, "");

                        // handle windows file system
                        pathWithInDir = pathWithInDir.Replace("\\", "/");

                        if (pathWithInDir[0] == '/') pathWithInDir = pathWithInDir.Substring(1);

                        return new LocalFile(f, pathWithInDir);
                    })
                .ToList();

            return localFiles;
        }
        catch (DirectoryNotFoundException)
        {
            logger.LogError("directory {} could not be found", directory);

            return new List<LocalFile>();
        }
    }

    static async Task<List<BuildFilesListResultsInner>> GetRemoteFiles(
        IGameServerHostingService service,
        string projectId,
        string environmentId,
        CreateBuild200Response build,
        CancellationToken cancellationToken
    )
    {
        var offset = 0;
        var remoteFiles = new List<BuildFilesListResultsInner>();
        while (true)
        {
            var buildsFileList = await GetBuildsFileList(
                service,
                projectId,
                environmentId,
                build.BuildID,
                Limit,
                offset,
                cancellationToken
            );

            if (buildsFileList.Results.Count == 0)
                break;

            remoteFiles.AddRange(buildsFileList.Results);

            if (buildsFileList.Results.Count < Limit)
                break;

            offset = buildsFileList.Offset + buildsFileList.Results.Count;
        }

        return remoteFiles;
    }

    static async Task<int> UploadFiles(
        IGameServerHostingService service,
        string projectId,
        string environmentId,
        CreateBuild200Response build,
        List<LocalFile> localFiles,
        HttpClient httpClient,
        CancellationToken cancellationToken
    )
    {
        var uploaded = 0;
        foreach (var fileToUpload in localFiles)
        {
            var localFile = SystemFile.OpenRead(fileToUpload.GetSystemPath());
            var remoteFile = await service.BuildsApi.CreateOrUpdateBuildFileAsync(
                Guid.Parse(projectId),
                Guid.Parse(environmentId),
                build.BuildID,
                new CreateOrUpdateBuildFileRequest(fileToUpload.GetPathInDirectory()),
                cancellationToken: cancellationToken
            );

            if (remoteFile.Uploaded)
                continue;

            if (remoteFile.SignedUrl == "")
                throw new InvalidResponseException(
                    $"signedUrl missing from entry with path {fileToUpload.GetPathInDirectory()} response");


            await httpClient.PutAsync(remoteFile.SignedUrl, new StreamContent(localFile), cancellationToken);
            uploaded++;
        }

        return uploaded;
    }

    static async Task<int> DeleteOldFiles(
        IGameServerHostingService service,
        string projectId,
        string environmentId,
        CreateBuild200Response build,
        IReadOnlyCollection<LocalFile> localFiles,
        CancellationToken cancellationToken
    )
    {
        var remoteFiles = await GetRemoteFiles(
            service,
            projectId,
            environmentId,
            build,
            cancellationToken);

        var filesToDelete = remoteFiles
            .Where(LocalFilesDoesNotContainEntry(localFiles));

        var deleted = 0;
        foreach (var f in filesToDelete)
        {
            await service.BuildsApi.DeleteBuildFileByPathAsync(
                Guid.Parse(projectId),
                Guid.Parse(environmentId),
                build.BuildID,
                f.Path,
                cancellationToken: cancellationToken
            );
            deleted++;
        }

        return deleted;
    }

    static async Task<BuildFilesList> GetBuildsFileList(
        IGameServerHostingService service,
        string projectId,
        string environmentId,
        long buildId,
        int limit,
        int offset,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var buildsFileListResponse = await service.BuildsApi.GetBuildFilesAsync(
                Guid.Parse(projectId),
                Guid.Parse(environmentId),
                buildId,
                limit,
                offset,
                cancellationToken: cancellationToken
            );

            return buildsFileListResponse;
        }
        catch (ApiException e)
        {
            if ((HttpStatusCode)e.ErrorCode != HttpStatusCode.Conflict)
                throw;

            // return empty list
            return new BuildFilesList(limit, offset, new List<BuildFilesListResultsInner>());
        }
    }

    static Func<BuildFilesListResultsInner, bool> LocalFilesDoesNotContainEntry(
        IReadOnlyCollection<LocalFile> localFiles
    )
    {
        return remoteEntry => localFiles.All(localFile => localFile.GetPathInDirectory() != remoteEntry.Path);
    }

    // We need to apply our own conditional validation based on the build type
    internal static void ValidateFileUploadInput(BuildCreateVersionInput input)
    {
        if (input.FileDirectory == null)
            throw new MissingInputException(BuildCreateVersionInput.FileDirectoryKey);
        if (input.ContainerTag != null)
            throw new CliException("Build does not support container flags.", ExitCode.HandledError);
        if (input.BucketUrl != null)
            throw new CliException("Build does not support s3 buckets.", ExitCode.HandledError);
    }
}
