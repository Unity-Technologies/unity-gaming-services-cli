namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeModulesDownloader : ICloudCodeModulesDownloader
{
    readonly HttpClient m_HttpClient;
    public CloudCodeModulesDownloader(HttpClient client) => m_HttpClient = client;

    public Task<Stream> DownloadModule(
        CloudCodeModule module,
        CancellationToken cancellationToken)
    {
        return m_HttpClient.GetStreamAsync(module.SignedUrl, cancellationToken);
    }
}
