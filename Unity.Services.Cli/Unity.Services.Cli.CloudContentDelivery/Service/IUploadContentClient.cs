using System.IO.Abstractions;

namespace Unity.Services.Cli.CloudContentDelivery.Service;

public interface IUploadContentClient
{
    string GetContentType(string localPath);

    Task<HttpResponseMessage> UploadContentToCcd(
        string signedUrl,
        FileSystemStream filestream,
        CancellationToken cancellationToken = default);

    string GetContentHash(FileSystemStream filestream);
    long GetContentSize(FileSystemStream filestream);
}
