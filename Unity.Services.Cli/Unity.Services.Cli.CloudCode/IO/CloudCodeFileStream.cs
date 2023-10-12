using System.IO.Abstractions;
using Unity.Services.CloudCode.Authoring.Editor.Core.IO;

namespace Unity.Services.Cli.CloudCode.IO;

class CloudCodeFileStream : IFileStream
{
    internal FileSystemStream FileStream;
    public CloudCodeFileStream(FileSystemStream fileStream)
    {
        FileStream = fileStream;
    }

    public void Close()
    {
        FileStream.Close();
    }
}
