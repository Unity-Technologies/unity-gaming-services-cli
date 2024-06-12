using System.IO.Abstractions;
using System.Security.Cryptography;

namespace Unity.Services.Cli.CloudContentDelivery.Service;

public class UploadContentClient : IUploadContentClient
{
    readonly HttpClient m_HttpClient;

    public UploadContentClient(HttpClient httpClient)
    {
        m_HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        // Because very large upload can take hours due to a poor connection, we have opted not to impose a timeout to give a chance to anyone to upload their content irrespective of the time it takes.
        m_HttpClient.Timeout = Timeout.InfiniteTimeSpan;
    }

    public string GetContentType(string localPath)
    {
        try
        {
            return MimeMapping.MimeUtility.GetMimeMapping(localPath);
        }
        catch (Exception)
        {
            return "application/octet-stream";
        }
    }

    public Task<HttpResponseMessage> UploadContentToCcd(
        string signedUrl,
        FileSystemStream filestream,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Put,
            RequestUri = new Uri(signedUrl)
        };

        filestream.Seek(0, SeekOrigin.Begin);
        var streamContent = new StreamContent(filestream);

        streamContent.Headers.Add("Content-Type", GetContentType(filestream.Name));
        streamContent.Headers.Add("Content-Length", filestream.Length.ToString());
        request.Content = streamContent;

        return m_HttpClient.PutAsync(signedUrl, streamContent, cancellationToken);
    }

    public string GetContentHash(FileSystemStream filestream)
    {
        var md5 = MD5.Create();
        return BitConverter.ToString(md5.ComputeHash(filestream)).Replace("-", string.Empty).ToLower();
    }

    public long GetContentSize(FileSystemStream filestream)
    {
        return filestream.Length;
    }
}
