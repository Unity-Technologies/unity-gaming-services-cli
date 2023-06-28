namespace Unity.Services.Cli.CloudCode.Deploy;

interface ICloudCodeModulesDownloader
{
    Task<Stream> DownloadModule(
        CloudCodeModule module,
        CancellationToken cancellationToken);
}
