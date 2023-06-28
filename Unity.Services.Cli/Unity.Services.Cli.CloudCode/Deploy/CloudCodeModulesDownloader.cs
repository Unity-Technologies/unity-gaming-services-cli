namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeModulesDownloader : ICloudCodeModulesDownloader
{
    readonly HttpClient _httpClient;
    public CloudCodeModulesDownloader(HttpClient client) => _httpClient = client;

    public Task<Stream> DownloadModule(
        CloudCodeModule module,
        CancellationToken cancellationToken)
    {
            return _httpClient.GetStreamAsync(module.SignedUrl, cancellationToken);
    }
}
